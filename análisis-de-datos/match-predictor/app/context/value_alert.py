"""
Alerta de valor: compara la probabilidad del modelo con la cuota de la casa.

Regla cardinal: la cuota NUNCA entra como input al cálculo de probabilidad.
Es solo el espejo contra el que se compara. Si se filtrara, el modelo copiaría
a la casa (exactamente lo que queremos evitar).

Patrón "favorito por historia": equipo con baja forma reciente pero cuota muy
baja → la casa lo hace favorito por nombre/historia, el modelo discrepa.
"""
from __future__ import annotations

from app.core.config import VALUE_ALERT_THRESHOLD
from app.core.logger import get_logger

log = get_logger(__name__)


def decimal_to_implied_prob(decimal_odds: float, overround: float = 1.05) -> float:
    """
    Convierte cuota decimal a probabilidad implícita, descontando el margen de la casa.
    overround: margen típico de la casa (1.05 = 5%). Se normaliza dividiendo por overround.
    """
    if decimal_odds <= 1.0:
        return 1.0
    raw = 1.0 / decimal_odds
    return min(raw / overround, 1.0)


def check_value(
    market: str,
    selection: str,
    model_prob: float,
    decimal_odds: float,
    forma_5: float = 0.0,
    historical_favorite: bool = False,
) -> dict | None:
    """
    Evalúa si hay valor en una pata.
    Retorna un dict de alerta o None si no hay valor.

    forma_5: señal de forma reciente del equipo favorito (-1..+1).
    historical_favorite: True si el equipo es históricamente favorito pero forma baja.
    """
    if decimal_odds <= 1.0:
        return None

    implied_prob = decimal_to_implied_prob(decimal_odds)
    diff = model_prob - implied_prob

    alert = None
    if diff >= VALUE_ALERT_THRESHOLD:
        alert = {
            "market": market,
            "selection": selection,
            "model_prob": round(model_prob, 4),
            "bookmaker_prob": round(implied_prob, 4),
            "decimal_odds": decimal_odds,
            "difference": round(diff, 4),
            "value_flag": f"VALOR: modelo {round(model_prob*100,1)}% vs casa {round(implied_prob*100,1)}%",
            "historical_overvaluation": False,
            "match_level": True,
            "message": (
                f"El modelo estima {round(model_prob*100,1)}% de probabilidad, "
                f"pero la casa paga {decimal_odds} ({round(implied_prob*100,1)}% implícito). "
                f"Diferencia de {round(diff*100,1)} puntos porcentuales a favor del modelo."
            ),
        }

    # Caso especial: favorito por historia con forma baja
    if historical_favorite and forma_5 < -0.1:
        msg = (
            f"La casa paga {decimal_odds} ({round(implied_prob*100,1)}%) al favorito por nombre; "
            f"el modelo estima {round(model_prob*100,1)}% según forma actual. "
            "Posible sobrevaloración histórica: el nombre infla la cuota, el presente dice otra cosa."
        )
        if alert:
            alert["historical_overvaluation"] = True
            alert["message"] = msg
        else:
            # Alerta informativa aunque no llegue al umbral de valor
            alert = {
                "market": market,
                "selection": selection,
                "model_prob": round(model_prob, 4),
                "bookmaker_prob": round(implied_prob, 4),
                "decimal_odds": decimal_odds,
                "difference": round(diff, 4),
                "value_flag": "ADVERTENCIA: favorito histórico con forma baja",
                "historical_overvaluation": True,
                "match_level": True,
                "message": msg,
            }

    return alert


def process_all_alerts(legs: list, odds: dict, forma_5: float = 0.0) -> list[dict]:
    """
    Genera alertas para todas las patas que tengan cuota disponible.
    legs: lista de LegResult.
    odds: dict con claves como 'home_decimal', 'away_decimal', 'draw_decimal'.
    """
    alerts = []
    odds_map = {
        "home": odds.get("home_decimal"),
        "away": odds.get("away_decimal"),
        "draw": odds.get("draw_decimal"),
    }

    for leg in legs:
        decimal = None
        market = leg.market
        sel = leg.selection.lower()

        if market == "match_result":
            if "gana" in sel and ("local" in sel or _is_home_keyword(sel)):
                decimal = odds_map.get("home")
            elif "gana" in sel:
                decimal = odds_map.get("away")
            elif "empate" in sel:
                decimal = odds_map.get("draw")

        if decimal and decimal > 1.0:
            alert = check_value(
                market=market,
                selection=leg.selection,
                model_prob=leg.probability,
                decimal_odds=decimal,
                forma_5=forma_5,
            )
            if alert:
                alerts.append(alert)

        # Añadir la cuota al leg.bookmaker para incluirla en la respuesta
        if decimal:
            implied = decimal_to_implied_prob(decimal)
            leg.bookmaker = {
                "implied_prob": round(implied, 4),
                "decimal_odds": decimal,
                "value_flag": (
                    f"VALOR: modelo {round(leg.probability*100,1)}% vs casa {round(implied*100,1)}%"
                    if leg.probability - implied >= VALUE_ALERT_THRESHOLD
                    else None
                ),
            }

    return alerts


def _is_home_keyword(text: str) -> bool:
    return any(w in text for w in ["local", "home", "casa"])
