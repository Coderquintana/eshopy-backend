"""
Simulación Monte Carlo para probabilidades CONJUNTAS coherentes.

Problema resuelto:
  Rankear patas por probabilidad individual e ignorar correlaciones produce
  combinadas incoherentes. Ejemplo real: "gol de Yamal", "gol de Oyarzabal" y
  "gol de Cucurella" pueden ser las 3 patas más probables individualmente, pero
  combinarlas requiere 3 goles de España en un partido donde el modelo espera 2.
  Multiplicar p1×p2×p3 como si fueran independientes da probabilidades falsas.

Solución:
  Simular el partido N veces generando resultados COHERENTES en cada simulación:
  el marcador determina cuántos goles hay, y esos goles se distribuyen entre
  jugadores con Multinomial. La prob. conjunta de una combinada = fracción de
  simulaciones donde TODAS las patas ocurren simultáneamente.

Diseño:
  - Numpy vectorizado cuando está disponible (máquinas modernas).
  - Fallback Python puro (random + Knuth) cuando numpy no carga (e.g., CPUs sin
    SSE4.2 que no pueden ejecutar los wheels precompilados de numpy 2.x).
  - La API es idéntica en ambos casos.
"""
from __future__ import annotations

import math
import random as _random
from dataclasses import dataclass, field
from itertools import combinations
from typing import TYPE_CHECKING

from app.core.logger import get_logger

if TYPE_CHECKING:
    from app.models.markets import LegResult

log = get_logger(__name__)

# ---------------------------------------------------------------------------
# Detección de numpy (RuntimeError si CPU no soporta las instrucciones requeridas)
# ---------------------------------------------------------------------------

try:
    import numpy as _np
    _NUMPY = True
    log.debug("simulation.py: numpy %s disponible (modo vectorizado).", _np.__version__)
except (ImportError, RuntimeError) as _np_err:
    _np = None  # type: ignore
    _NUMPY = False
    log.info(
        "simulation.py: numpy no disponible (%s). Usando Python puro — más lento "
        "pero matemáticamente equivalente.", _np_err
    )


# ---------------------------------------------------------------------------
# Samplers Poisson y Multinomial en Python puro
# ---------------------------------------------------------------------------

def _poisson_sample_pure(mu: float) -> int:
    """
    Algoritmo de Knuth para Poisson exacto (mu < 30).
    Aproximación Normal para mu >= 30 (extremadamente raro en goles de fútbol).
    """
    if mu <= 0:
        return 0
    if mu >= 30:
        return max(0, round(_random.gauss(mu, math.sqrt(mu))))
    L = math.exp(-mu)
    k, p = 0, 1.0
    while p > L:
        k += 1
        p *= _random.random()
    return k - 1


def _poisson_vector_pure(mu: float, n: int) -> list[int]:
    return [_poisson_sample_pure(mu) for _ in range(n)]


def _multinomial_pure(total: int, probs: list[float]) -> list[int]:
    """
    Multinomial(total, probs) via Binomiales sucesivas.
    Distribuye `total` goles entre jugadores según sus tasas.
    """
    if total <= 0:
        return [0] * len(probs)
    result = [0] * len(probs)
    remaining = total
    p_remaining = 1.0
    for i in range(len(probs) - 1):
        if remaining <= 0 or p_remaining < 1e-12:
            break
        p = min(probs[i] / p_remaining, 1.0)
        # Binomial(remaining, p) via suma de Bernoulli
        k = sum(1 for _ in range(remaining) if _random.random() < p)
        result[i] = k
        remaining -= k
        p_remaining -= probs[i]
    result[-1] = remaining
    return result


# ---------------------------------------------------------------------------
# Resultado de una simulación
# ---------------------------------------------------------------------------

@dataclass
class SimulationResult:
    n_simulations: int
    home_goals: list[int]           # longitud N: goles del local en cada simulación
    away_goals: list[int]           # longitud N
    total_goals: list[int]          # home + away
    total_corners: list[int]
    total_cards: list[int]
    # home_first[i] = True si el local marcó primero en la simulación i
    # None si ningún equipo marcó
    home_first: list[bool | None]
    # goals por jugador: {nombre: lista de longitud N con goles en cada simulación}
    player_goals: dict[str, list[int]] = field(default_factory=dict)
    # Metadatos útiles para validación
    lambda_home: float = 1.3
    lambda_away: float = 1.0

    def avg_goals(self) -> float:
        return sum(self.total_goals) / max(self.n_simulations, 1)

    def avg_corners(self) -> float:
        return sum(self.total_corners) / max(self.n_simulations, 1)


# ---------------------------------------------------------------------------
# Función principal de simulación
# ---------------------------------------------------------------------------

def simulate_match(model_dict: dict, n_simulations: int = 5_000) -> SimulationResult:
    """
    Simula el partido N veces generando resultados coherentes.

    Coherencia garantizada:
      - El marcador (home_goals, away_goals) se muestrea de Poisson bivariado.
      - Los goles de jugadores individuales se distribuyen via Multinomial
        condicionado al marcador → la suma de goles de jugadores = marcador del equipo.
        Esto hace que "Yamal + Oyarzabal + Cucurella = 3 goles de España" sea
        automáticamente imposible si el marcador simula 2-1.

    Args:
        model_dict: diccionario de to_model_dict() de MatchData.
        n_simulations: número de simulaciones (5.000 por defecto ≈ 0.1s puro Python).
    """
    lh = float(model_dict.get("lambda_home", 1.3))
    la = float(model_dict.get("lambda_away", 1.0))
    lc = float(model_dict.get("lambda_corners_home", 5.5)) + float(model_dict.get("lambda_corners_away", 4.5))
    lk = float(model_dict.get("lambda_cards_home", 2.0)) + float(model_dict.get("lambda_cards_away", 1.8))
    players = model_dict.get("players", [])

    log.debug("Simulando %d partidos (λ_h=%.2f, λ_a=%.2f, numpy=%s)", n_simulations, lh, la, _NUMPY)

    if _NUMPY:
        home_goals, away_goals, total_corners, total_cards = _sample_vectors_numpy(
            lh, la, lc, lk, n_simulations
        )
    else:
        home_goals = _poisson_vector_pure(lh, n_simulations)
        away_goals = _poisson_vector_pure(la, n_simulations)
        total_corners = _poisson_vector_pure(lc, n_simulations)
        total_cards = _poisson_vector_pure(lk, n_simulations)

    total_goals = [h + a for h, a in zip(home_goals, away_goals)]

    # Quién marcó primero: proporción lh/(lh+la), condicionado a que hubo goles
    p_home_first = lh / (lh + la) if (lh + la) > 0 else 0.5
    home_first: list[bool | None] = []
    for tg in total_goals:
        if tg == 0:
            home_first.append(None)
        else:
            home_first.append(_random.random() < p_home_first)

    # Distribución coherente de goles a jugadores
    player_goals = _distribute_goals_to_players(
        home_goals, away_goals, players, lh, la
    )

    return SimulationResult(
        n_simulations=n_simulations,
        home_goals=home_goals,
        away_goals=away_goals,
        total_goals=total_goals,
        total_corners=total_corners,
        total_cards=total_cards,
        home_first=home_first,
        player_goals=player_goals,
        lambda_home=lh,
        lambda_away=la,
    )


def _sample_vectors_numpy(lh, la, lc, lk, n):
    """Sampling vectorizado con numpy. Solo se llama si numpy está disponible."""
    home_goals = _np.random.poisson(lh, n).tolist()
    away_goals = _np.random.poisson(la, n).tolist()
    total_corners = _np.random.poisson(lc, n).tolist()
    total_cards = _np.random.poisson(lk, n).tolist()
    return home_goals, away_goals, total_corners, total_cards


def _distribute_goals_to_players(
    home_goals: list[int],
    away_goals: list[int],
    players: list[dict],
    lh: float,
    la: float,
) -> dict[str, list[int]]:
    """
    Distribuye los goles del equipo entre jugadores via Multinomial.

    Por qué Multinomial y no Poisson independiente:
      Si sampleamos los goles de cada jugador con Poisson independiente, la suma
      puede exceder los goles del equipo. Con Multinomial(total_goles, probs_jugadores)
      la suma = total_goles por construcción → la correlación entre jugadores y con
      el marcador queda incorporada automáticamente.
    """
    if not players:
        return {}

    n = len(home_goals)
    home_players = [p for p in players if p.get("team") == "home"]
    away_players = [p for p in players if p.get("team") != "home"]

    result: dict[str, list[int]] = {}

    def _compute_player_probs(team_players: list[dict], team_lambda: float) -> list[float]:
        """Normaliza las tasas de gol individuales a probabilidades para Multinomial."""
        rates = [
            (p.get("goals_per_90", 0.3) / 90.0) * p.get("expected_minutes", 90)
            for p in team_players
        ]
        # La fracción "otros goleadores" que no están en la lista
        named_rate = sum(rates)
        team_rate = max(team_lambda, 0.01)
        # Si los jugadores listados no cubren toda la tasa del equipo, el resto va a "otros"
        others_rate = max(0.0, team_rate - named_rate)
        all_rates = rates + [others_rate]
        total = sum(all_rates)
        if total <= 0:
            return [1.0 / len(all_rates)] * len(all_rates)
        return [r / total for r in all_rates]

    def _allocate(team_goals: list[int], team_players: list[dict], probs: list[float]):
        player_arrays = {p["name"]: [0] * n for p in team_players}
        # Agrupar simulaciones por número de goles (optimización)
        by_count: dict[int, list[int]] = {}
        for i, k in enumerate(team_goals):
            by_count.setdefault(k, []).append(i)

        for k, indices in by_count.items():
            if k == 0:
                continue
            if _NUMPY:
                samples = _np.random.multinomial(k, probs, size=len(indices)).tolist()
                for j, sim_idx in enumerate(indices):
                    for p_idx, player in enumerate(team_players):
                        player_arrays[player["name"]][sim_idx] = samples[j][p_idx]
            else:
                for sim_idx in indices:
                    alloc = _multinomial_pure(k, probs)
                    for p_idx, player in enumerate(team_players):
                        player_arrays[player["name"]][sim_idx] = alloc[p_idx]
        return player_arrays

    if home_players:
        probs_h = _compute_player_probs(home_players, lh)
        result.update(_allocate(home_goals, home_players, probs_h))

    if away_players:
        probs_a = _compute_player_probs(away_players, la)
        result.update(_allocate(away_goals, away_players, probs_a))

    return result


# ---------------------------------------------------------------------------
# Evaluación de una pata contra el resultado de una simulación
# ---------------------------------------------------------------------------

def evaluate_leg(condition: dict, sim: SimulationResult) -> list[bool]:
    """
    Dado un `condition` dict (del campo LegResult.condition), retorna una lista
    de booleanos de longitud N: True si la pata ocurrió en esa simulación.
    """
    t = condition.get("type")
    n = sim.n_simulations

    if t == "result":
        outcome = condition["outcome"]
        if outcome == "home":
            return [h > a for h, a in zip(sim.home_goals, sim.away_goals)]
        elif outcome == "draw":
            return [h == a for h, a in zip(sim.home_goals, sim.away_goals)]
        elif outcome == "away":
            return [a > h for h, a in zip(sim.home_goals, sim.away_goals)]
        elif outcome == "home_or_draw":
            return [h >= a for h, a in zip(sim.home_goals, sim.away_goals)]
        elif outcome == "away_or_draw":
            return [a >= h for h, a in zip(sim.home_goals, sim.away_goals)]

    elif t == "over_under":
        event = condition["event"]
        line = float(condition["line"])
        direction = condition["direction"]
        if event == "goals":
            series = sim.total_goals
        elif event == "corners":
            series = sim.total_corners
        elif event == "cards":
            series = sim.total_cards
        else:
            return [False] * n
        if direction == "over":
            return [v > line for v in series]
        else:
            return [v <= line for v in series]

    elif t == "btts":
        val = condition["value"]
        if val:
            return [h > 0 and a > 0 for h, a in zip(sim.home_goals, sim.away_goals)]
        else:
            return [h == 0 or a == 0 for h, a in zip(sim.home_goals, sim.away_goals)]

    elif t == "first_score":
        team = condition["team"]
        if team == "home":
            return [hf is True for hf in sim.home_first]
        else:
            return [hf is False for hf in sim.home_first]

    elif t in ("goalscorer", "first_goalscorer"):
        player_name = condition.get("player", "")
        goals = sim.player_goals.get(player_name)
        if goals is None:
            return [False] * n
        if t == "goalscorer":
            return [g > 0 for g in goals]
        else:
            # Primer goleador: el jugador marcó Y fue el primero del equipo
            team = condition.get("team", "home")
            first_mask = [hf is True for hf in sim.home_first] if team == "home" else [hf is False for hf in sim.home_first]
            return [g > 0 and f for g, f in zip(goals, first_mask)]

    log.warning("evaluate_leg: tipo desconocido '%s'", t)
    return [False] * n


def joint_probability(conditions: list[dict], sim: SimulationResult) -> float:
    """
    Probabilidad conjunta de varias patas via simulación.
    = fracción de simulaciones donde TODAS las condiciones son True simultáneamente.
    """
    if not conditions:
        return 1.0
    n = sim.n_simulations
    mask = [True] * n
    for cond in conditions:
        leg_mask = evaluate_leg(cond, sim)
        mask = [m and lm for m, lm in zip(mask, leg_mask)]
    count = sum(mask)
    return count / n


# ---------------------------------------------------------------------------
# Detección de contradicciones
# ---------------------------------------------------------------------------

def are_contradictory(cond_a: dict, cond_b: dict) -> bool:
    """
    Retorna True si dos condiciones son lógicamente incompatibles.
    Ejemplos: over 2.5 + under 2.5, btts sí + btts no, home wins + away wins.
    """
    ta, tb = cond_a.get("type"), cond_b.get("type")

    # Mismo tipo over_under, mismo evento, misma línea, dirección opuesta
    if ta == "over_under" and tb == "over_under":
        if (cond_a.get("event") == cond_b.get("event") and
                cond_a.get("line") == cond_b.get("line") and
                cond_a.get("direction") != cond_b.get("direction")):
            return True

    # btts sí + btts no
    if ta == "btts" and tb == "btts":
        if cond_a.get("value") != cond_b.get("value"):
            return True

    # Resultado: ganó local + ganó visitante, etc.
    if ta == "result" and tb == "result":
        mutual_exclusive = {
            frozenset({"home", "away"}),
            frozenset({"home", "draw"}),
            frozenset({"away", "draw"}),
            frozenset({"home_or_draw", "away"}),
            frozenset({"away_or_draw", "home"}),
        }
        pair = frozenset({cond_a.get("outcome"), cond_b.get("outcome")})
        if pair in mutual_exclusive:
            return True

    # btts sí + under 0.5 goles (o similar)
    if ta == "btts" and cond_a.get("value") is True and tb == "over_under":
        if cond_b.get("event") == "goals" and cond_b.get("direction") == "under":
            if float(cond_b.get("line", 99)) <= 1.5:
                return True
    if tb == "btts" and cond_b.get("value") is True and ta == "over_under":
        if cond_a.get("event") == "goals" and cond_a.get("direction") == "under":
            if float(cond_a.get("line", 99)) <= 1.5:
                return True

    return False


def _combo_is_valid(conditions: list[dict]) -> bool:
    """Verifica que ningún par de condiciones en la combinada sea contradictorio."""
    for i, ca in enumerate(conditions):
        for cb in conditions[i + 1:]:
            if are_contradictory(ca, cb):
                return False
    return True


# ---------------------------------------------------------------------------
# Familias de mercado para evitar combos redundantes
# ---------------------------------------------------------------------------

# Agrupa mercados por "familia semántica". El combo evita poner dos patas de la
# misma familia (e.g., 'Gana local' + 'Local o empate' + 'Local marca primero'
# son tres formas de decir lo mismo → combinada sin valor).
_MARKET_FAMILY: dict[str, str] = {
    "match_result":       "result",
    "double_chance":      "result",    # versión relajada del resultado
    "first_to_score":     "result",    # muy correlacionado con quién gana
    "goals_over_under":   "goals",
    "btts":               "goals",
    "corners_over_under": "corners",
    "cards_over_under":   "cards",
    "goalscorer_anytime": "scorer",
    "first_goalscorer":   "scorer",
}


def _combo_is_diverse(combo_legs: list) -> bool:
    """
    Retorna True si ningún par de patas pertenece a la misma familia de mercado.
    Previene combos redundantes: 'Gana local' + 'Local o empate' + 'Local marca primero'
    tienen familia 'result' los tres → el combo no aporta información independiente.
    """
    families = [_MARKET_FAMILY.get(l.market, l.market) for l in combo_legs]
    return len(families) == len(set(families))


# ---------------------------------------------------------------------------
# Selección óptima del combo via simulación
# ---------------------------------------------------------------------------

def find_best_combo(
    legs: list["LegResult"],
    sim: SimulationResult,
    top_n: int = 3,
    n_candidates: int = 8,
    include_low: bool = False,
) -> dict:
    """
    Selecciona la combinada de `top_n` patas con MÁXIMA probabilidad conjunta,
    descartando combinadas contradictorias o redundantes.

    Algoritmo:
      1. Tomar los top `n_candidates` legs evaluables por probabilidad individual.
      2. Primera pasada: solo combos donde cada pata es de una familia de mercado
         distinta (resultado / goles / córners / tarjetas / goleador).
         Esto evita combos redundantes como 'Gana local' + 'Local o empate'.
      3. Si ningún combo diverso es válido (mercados insuficientes), segunda pasada
         sin restricción de diversidad con aviso en la respuesta.
      4. Retornar la combinación con mayor prob. conjunta.

    C(8, 3) = 56 combinaciones × 5.000 simulaciones = rápido en Python puro.
    """
    # Filtrar legs: solo los que tienen condition y no son low (salvo que se pidan)
    evaluable = [
        l for l in legs
        if l.condition is not None and (include_low or l.confidence != "low")
    ]
    # Ordenar por probabilidad individual y tomar top n_candidates
    candidates = sorted(evaluable, key=lambda x: x.probability, reverse=True)[:n_candidates]

    if not candidates:
        return _fallback_combo(legs, top_n, include_low)

    best_prob = -1.0
    best_combo_legs: list = []
    diverse_found = False

    # Primera pasada: forzar diversidad de familias de mercado
    for combo in combinations(candidates, min(top_n, len(candidates))):
        conditions = [l.condition for l in combo if l.condition]
        if not _combo_is_valid(conditions):
            continue
        if not _combo_is_diverse(list(combo)):
            continue
        jp = joint_probability(conditions, sim)
        if jp > best_prob:
            best_prob = jp
            best_combo_legs = list(combo)
            diverse_found = True

    # Segunda pasada: fallback sin restricción de diversidad si no hubo combo válido
    if not best_combo_legs:
        for combo in combinations(candidates, min(top_n, len(candidates))):
            conditions = [l.condition for l in combo if l.condition]
            if not _combo_is_valid(conditions):
                continue
            jp = joint_probability(conditions, sim)
            if jp > best_prob:
                best_prob = jp
                best_combo_legs = list(combo)

    if not best_combo_legs:
        return _fallback_combo(legs, top_n, include_low)

    # Prob. individual multiplicada (para comparar con la real)
    naive_product = 1.0
    for l in best_combo_legs:
        naive_product *= l.probability

    diversity_note = (
        "Combinada diversificada (mercados distintos)."
        if diverse_found
        else "Aviso: mercados insuficientes para diversificar — patas de familias similares."
    )

    return {
        "legs": [l.rank for l in best_combo_legs],
        "combined_probability": round(best_prob, 4),
        "naive_product": round(naive_product, 4),
        "correlation_discount": round(naive_product - best_prob, 4),
        "diverse": diverse_found,
        "note": (
            f"Probabilidad conjunta real (Monte Carlo, {sim.n_simulations:,} sim.): {round(best_prob, 4)}. "
            f"Producto ingenuo: {round(naive_product, 4)}. "
            f"{diversity_note}"
        ),
        "selections": [
            {"rank": l.rank, "market": l.market, "selection": l.selection,
             "individual_prob": round(l.probability, 4)}
            for l in best_combo_legs
        ],
    }


def _fallback_combo(legs: list, top_n: int, include_low: bool) -> dict:
    """Fallback si no hay legs evaluables en simulación: producto simple."""
    candidates = [l for l in legs if include_low or l.confidence != "low"][:top_n]
    product = 1.0
    for l in candidates:
        product *= l.probability
    return {
        "legs": [l.rank for l in candidates],
        "combined_probability": round(product, 4),
        "naive_product": round(product, 4),
        "correlation_discount": 0.0,
        "note": "Producto directo (sin simulación disponible).",
        "selections": [
            {"rank": l.rank, "market": l.market, "selection": l.selection,
             "individual_prob": round(l.probability, 4)}
            for l in candidates
        ],
    }
