"""
Cada "pata" (mercado de apuesta) es una función que recibe MatchData ya normalizado
y devuelve una lista de LegResult con probabilidad, confianza y drivers.

Nivel de confianza (del plan):
  high   → resultado, goles, córners
  medium → tarjetas, primero en marcar, tapadas, goleador anytime
  low    → primer goleador exacto, asistencia puntual
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Optional

from app.models import poisson as pm
from app.models.weights import get_neutral_weights, get_weights


@dataclass
class Driver:
    factor: str
    weight: float
    signal: str  # "+" | "-" | "~"
    value: Optional[float] = None


@dataclass
class LegResult:
    market: str
    selection: str
    probability: float
    confidence: str          # "high" | "medium" | "low"
    drivers: list[Driver] = field(default_factory=list)
    bookmaker: Optional[dict] = None
    rank: Optional[int] = None
    # Condición estructurada para evaluación en simulación Monte Carlo.
    # No se serializa en to_dict(); es uso interno de simulation.py.
    # Esquema: {"type": "result"|"over_under"|"btts"|"first_score"|"goalscorer",
    #           ...parámetros según tipo...}
    condition: Optional[dict] = field(default=None, compare=False, repr=False)

    def to_dict(self) -> dict:
        return {
            "rank": self.rank,
            "market": self.market,
            "selection": self.selection,
            "probability": round(self.probability, 4),
            "confidence": self.confidence,
            "drivers": [
                {
                    "factor": d.factor,
                    "weight": d.weight,
                    "signal": d.signal,
                    "value": d.value,
                }
                for d in self.drivers
            ],
            "bookmaker": self.bookmaker or {},
        }


# ---------------------------------------------------------------------------
# Mercados HIGH
# ---------------------------------------------------------------------------

def market_match_result(data: dict) -> list[LegResult]:
    """
    Resultado 1X2 usando Poisson bivariado con corrección Dixon-Coles.
    También calcula doble oportunidad (1X, X2, 12).
    """
    lh = data.get("lambda_home", 1.3)
    la = data.get("lambda_away", 1.0)
    probs = pm.prob_result(lh, la)

    weights = (
        get_neutral_weights("match_result")
        if data.get("neutral_venue")
        else get_weights("match_result")
    )
    drivers = _build_drivers_result(data, weights)

    legs = [
        LegResult(
            market="match_result",
            selection=f"Gana {data.get('home_team', 'Local')}",
            probability=probs["home"],
            confidence="high",
            drivers=drivers,
            condition={"type": "result", "outcome": "home"},
        ),
        LegResult(
            market="match_result",
            selection="Empate",
            probability=probs["draw"],
            confidence="high",
            drivers=drivers,
            condition={"type": "result", "outcome": "draw"},
        ),
        LegResult(
            market="match_result",
            selection=f"Gana {data.get('away_team', 'Visitante')}",
            probability=probs["away"],
            confidence="high",
            drivers=drivers,
            condition={"type": "result", "outcome": "away"},
        ),
        # Doble oportunidad
        LegResult(
            market="double_chance",
            selection=f"{data.get('home_team','Local')} o Empate (1X)",
            probability=probs["home"] + probs["draw"],
            confidence="high",
            drivers=drivers,
            condition={"type": "result", "outcome": "home_or_draw"},
        ),
        LegResult(
            market="double_chance",
            selection=f"{data.get('away_team','Visitante')} o Empate (X2)",
            probability=probs["away"] + probs["draw"],
            confidence="high",
            drivers=drivers,
            condition={"type": "result", "outcome": "away_or_draw"},
        ),
    ]
    return legs


def market_goals(data: dict, line: float = 2.5) -> list[LegResult]:
    """Over/Under goles totales y BTTS."""
    lh = data.get("lambda_home", 1.3)
    la = data.get("lambda_away", 1.0)
    ou = pm.prob_goals_over_under(lh, la, line)
    btts = pm.prob_both_score(lh, la)

    drivers = _build_drivers_goals(data)
    return [
        LegResult(
            market="goals_over_under",
            selection=f"Más de {line} goles",
            probability=ou["over"],
            confidence="high",
            drivers=drivers,
            condition={"type": "over_under", "event": "goals", "line": line, "direction": "over"},
        ),
        LegResult(
            market="goals_over_under",
            selection=f"Menos de {line} goles",
            probability=ou["under"],
            confidence="high",
            drivers=drivers,
            condition={"type": "over_under", "event": "goals", "line": line, "direction": "under"},
        ),
        LegResult(
            market="btts",
            selection="Ambos marcan - Sí",
            probability=btts,
            confidence="high",
            drivers=drivers,
            condition={"type": "btts", "value": True},
        ),
        LegResult(
            market="btts",
            selection="Ambos marcan - No",
            probability=1.0 - btts,
            confidence="high",
            drivers=drivers,
            condition={"type": "btts", "value": False},
        ),
    ]


def market_corners(data: dict, line: float = 9.5) -> list[LegResult]:
    """Over/Under córners totales."""
    lc_h = data.get("lambda_corners_home", 5.5)
    lc_a = data.get("lambda_corners_away", 4.5)
    total_lambda = lc_h + lc_a
    ou = pm.prob_corners_over_under(total_lambda, line)

    drivers = _build_drivers_corners(data)
    return [
        LegResult(
            market="corners_over_under",
            selection=f"Más de {line} córners",
            probability=ou["over"],
            confidence="high",
            drivers=drivers,
            condition={"type": "over_under", "event": "corners", "line": line, "direction": "over"},
        ),
        LegResult(
            market="corners_over_under",
            selection=f"Menos de {line} córners",
            probability=ou["under"],
            confidence="high",
            drivers=drivers,
            condition={"type": "over_under", "event": "corners", "line": line, "direction": "under"},
        ),
    ]


# ---------------------------------------------------------------------------
# Mercados MEDIUM
# ---------------------------------------------------------------------------

def market_cards(data: dict, line: float = 3.5) -> list[LegResult]:
    """Over/Under tarjetas totales. Peso fuerte del árbitro."""
    lc_h = data.get("lambda_cards_home", 2.0)
    lc_a = data.get("lambda_cards_away", 1.8)
    total_lambda = lc_h + lc_a
    ou = pm.prob_cards_over_under(total_lambda, line)

    drivers = _build_drivers_cards(data)
    return [
        LegResult(
            market="cards_over_under",
            selection=f"Más de {line} tarjetas",
            probability=ou["over"],
            confidence="medium",
            drivers=drivers,
            condition={"type": "over_under", "event": "cards", "line": line, "direction": "over"},
        ),
        LegResult(
            market="cards_over_under",
            selection=f"Menos de {line} tarjetas",
            probability=ou["under"],
            confidence="medium",
            drivers=drivers,
            condition={"type": "over_under", "event": "cards", "line": line, "direction": "under"},
        ),
    ]


def market_first_to_score(data: dict) -> list[LegResult]:
    """
    Equipo que marca primero. Derivado de la fuerza ofensiva relativa y el ritmo.
    Aproximación: proporcional a las lambdas de goles de cada equipo.
    """
    lh = data.get("lambda_home", 1.3)
    la = data.get("lambda_away", 1.0)
    total = lh + la
    p_home_first = lh / total
    p_away_first = la / total
    # P(ninguno marca primero en los 90 min) ≈ muy baja; se ignora para simplificar

    drivers = [
        Driver("lambda_home", 0.5, "+" if lh > la else "-", lh),
        Driver("lambda_away", 0.5, "+" if la > lh else "-", la),
    ]
    return [
        LegResult(
            market="first_to_score",
            selection=f"{data.get('home_team','Local')} marca primero",
            probability=p_home_first,
            confidence="medium",
            drivers=drivers,
            condition={"type": "first_score", "team": "home"},
        ),
        LegResult(
            market="first_to_score",
            selection=f"{data.get('away_team','Visitante')} marca primero",
            probability=p_away_first,
            confidence="medium",
            drivers=drivers,
            condition={"type": "first_score", "team": "away"},
        ),
    ]


def market_goalscorer_anytime(data: dict) -> list[LegResult]:
    """
    Goleador anota (anytime) para los jugadores clave incluidos en data.
    data['players'] = lista de dicts con goals_per_90, expected_minutes, name, team.
    """
    players = data.get("players", [])
    if not players:
        return []

    legs = []
    for player in players:
        prob = pm.prob_goalscorer(
            goals_per_90=player.get("goals_per_90", 0.3),
            expected_minutes=player.get("expected_minutes", 90),
            team_goals_expected=data.get("lambda_home", 1.3)
            if player.get("team") == "home"
            else data.get("lambda_away", 1.0),
            team_avg_goals=player.get("team_avg_goals", 1.3),
        )
        drivers = [
            Driver("tasa_gol_90", 0.45, "+", player.get("goals_per_90")),
            Driver("minutos_esperados", 0.25, "+" if player.get("expected_minutes", 90) >= 70 else "-", player.get("expected_minutes")),
            Driver("fuerza_defensiva_rival", 0.20, "~", None),
            Driver("penales", 0.10, "+" if player.get("penalty_taker") else "~", None),
        ]
        legs.append(
            LegResult(
                market="goalscorer_anytime",
                selection=f"{player['name']} anota",
                probability=prob,
                confidence="medium",
                drivers=drivers,
                condition={
                    "type": "goalscorer",
                    "player": player["name"],
                    "team": player.get("team", "home"),
                    "goals_per_90": player.get("goals_per_90", 0.3),
                    "expected_minutes": player.get("expected_minutes", 90),
                    "team_avg_goals": player.get("team_avg_goals", 1.3),
                },
            )
        )
    return legs


# ---------------------------------------------------------------------------
# Mercados LOW
# ---------------------------------------------------------------------------

def market_first_goalscorer(data: dict) -> list[LegResult]:
    """
    Primer goleador exacto. MUY alta varianza; se marca confidence=low.
    Distribución del anytime de cada jugador ponderada por probabilidad de
    que marque en los primeros minutos (aproximación uniforme en 90 min).
    """
    anytime_legs = market_goalscorer_anytime(data)
    legs = []
    for leg in anytime_legs:
        # El primer goleador de los N candidatos es aprox prob_anytime / N
        # Esta es la simplificación honesta: confianza low.
        n_players = max(len(anytime_legs), 1)
        first_prob = leg.probability / n_players
        original_cond = leg.condition or {}
        legs.append(
            LegResult(
                market="first_goalscorer",
                selection=leg.selection.replace("anota", "primer goleador"),
                probability=first_prob,
                confidence="low",
                drivers=leg.drivers,
                condition={**original_cond, "type": "first_goalscorer"},
            )
        )
    return legs


# ---------------------------------------------------------------------------
# Función principal: calcula todos los mercados y los rankea
# ---------------------------------------------------------------------------

def calculate_all_markets(data: dict, include_low: bool = False) -> list[LegResult]:
    """
    Calcula todos los mercados, filtra por confianza y rankea por probabilidad descendente.
    """
    all_legs: list[LegResult] = []

    # HIGH
    all_legs.extend(market_match_result(data))
    all_legs.extend(market_goals(data, line=data.get("goals_line", 2.5)))
    all_legs.extend(market_corners(data, line=data.get("corners_line", 9.5)))

    # MEDIUM
    all_legs.extend(market_cards(data, line=data.get("cards_line", 3.5)))
    all_legs.extend(market_first_to_score(data))
    all_legs.extend(market_goalscorer_anytime(data))

    # LOW (sólo si se pide explícitamente)
    if include_low:
        all_legs.extend(market_first_goalscorer(data))

    # Rankear por probabilidad descendente
    sorted_legs = sorted(all_legs, key=lambda x: x.probability, reverse=True)
    for i, leg in enumerate(sorted_legs, 1):
        leg.rank = i

    return sorted_legs


def suggested_combo(legs: list[LegResult], top_n: int = 3,
                    include_low: bool = False) -> dict:
    """
    Selecciona las top_n patas de mayor probabilidad y calcula la probabilidad combinada.
    Excluye patas low salvo que include_low=True.
    EXPONE la matemática al usuario: no oculta que las combinadas se multiplican.
    """
    candidates = [l for l in legs if include_low or l.confidence != "low"]
    selected = candidates[:top_n]
    combined = 1.0
    for leg in selected:
        combined *= leg.probability
    return {
        "legs": [l.rank for l in selected],
        "combined_probability": round(combined, 4),
        "note": (
            f"Prob. combinada real de las {len(selected)} patas seleccionadas. "
            f"Recuerda: {' × '.join([str(round(l.probability, 2)) for l in selected])} = {round(combined, 4)}"
        ),
    }


# ---------------------------------------------------------------------------
# Helpers para construir drivers con sus pesos y señales
# ---------------------------------------------------------------------------

def _build_drivers_result(data: dict, weights: dict) -> list[Driver]:
    drivers = []
    fr = data.get("fuerza_relativa", 0.0)
    drivers.append(Driver("fuerza_relativa", weights.get("fuerza_relativa", 0.30),
                          "+" if fr > 0 else ("-" if fr < 0 else "~"), fr))
    forma = data.get("forma_5", 0.0)
    drivers.append(Driver("forma_5", weights.get("forma_5", 0.20),
                          "+" if forma > 0 else ("-" if forma < 0 else "~"), forma))
    bajas = data.get("bajas_clave", 0.0)
    drivers.append(Driver("bajas_clave", weights.get("bajas_clave", 0.15),
                          "-" if bajas < 0 else "~", bajas))
    h2h = data.get("h2h_advantage", 0.0)
    drivers.append(Driver("h2h", weights.get("h2h", 0.10),
                          "+" if h2h > 0 else ("-" if h2h < 0 else "~"), h2h))
    loc = data.get("localía", 0.0)
    drivers.append(Driver("localía", weights.get("localía", 0.10),
                          "+" if loc > 0 else "~", loc))
    imp = data.get("importancia", 0.0)
    drivers.append(Driver("importancia", weights.get("importancia", 0.15),
                          "~", imp))
    return drivers


def _build_drivers_goals(data: dict) -> list[Driver]:
    w = get_weights("goals")
    xg = data.get("xg_home", 0) + data.get("xg_away", 0)
    return [
        Driver("xg_total", w.get("xg", 0.40), "+" if xg > 2.5 else "-", xg),
        Driver("forma_goles", w.get("forma_goles", 0.25), "~", data.get("forma_goles")),
        Driver("fuerza_relativa", w.get("fuerza_relativa", 0.20), "~", None),
        Driver("bajas_ofensivas", w.get("bajas_ofensivas", 0.15), "~", None),
    ]


def _build_drivers_corners(data: dict) -> list[Driver]:
    w = get_weights("corners")
    avg = data.get("avg_corners") or 0.0
    return [
        Driver("media_corners", w.get("media_corners", 0.45), "+" if avg > 9 else "-", avg),
        Driver("forma_corners", w.get("forma_corners", 0.25), "~", None),
        Driver("favoritismo", w.get("favoritismo", 0.20), "~", None),
        Driver("arbitro_ritmo", w.get("arbitro_ritmo", 0.10), "~", None),
    ]


def _build_drivers_cards(data: dict) -> list[Driver]:
    w = get_weights("cards")
    ref = data.get("referee_cards_avg", None)
    return [
        Driver("arbitro", w.get("arbitro", 0.35), "~" if ref is None else ("+" if ref > 4 else "-"), ref),
        Driver("media_tarjetas_equipos", w.get("media_tarjetas_equipos", 0.30), "~", None),
        Driver("rivalidad", w.get("rivalidad", 0.20), "~", None),
        Driver("estilo_agresivo", w.get("estilo_agresivo", 0.15), "~", None),
    ]
