"""
Pesos iniciales por mercado. Son la HIPÓTESIS de partida.
El endpoint /patterns los recalibra con datos reales.
Se guardan versionados en la tabla weight_versions.

Diseño intencional: diccionarios, NO constantes hardcodeadas en la lógica.
Así /patterns puede proponer pesos nuevos sin tocar el motor.
"""

# Resultado 1X2 / clasificación
WEIGHTS_RESULT = {
    "fuerza_relativa": 0.30,    # Rating tipo Elo o valor de mercado
    "forma_5": 0.20,            # Últimos 5 partidos ponderados por calidad de rival
    "bajas_clave": 0.15,        # Lesiones/suspensiones de jugadores importantes
    "h2h": 0.10,                # Historial directo con decaimiento temporal
    "localía": 0.10,            # Local / visitante / neutral
    "importancia": 0.15,        # Necesidad de resultado (ya clasificado, rotación)
}

# Córners
WEIGHTS_CORNERS = {
    "media_corners": 0.45,      # Córners a favor/en contra por partido (media histórica)
    "forma_corners": 0.25,      # Forma reciente en córners (últimos 5)
    "favoritismo": 0.20,        # Favorito ataca más → más córners
    "arbitro_ritmo": 0.10,      # Árbitro y ritmo esperado del partido
}

# Tarjetas — el árbitro pesa muchísimo aquí
WEIGHTS_CARDS = {
    "arbitro": 0.35,            # Media de tarjetas por partido del árbitro
    "media_tarjetas_equipos": 0.30,  # Media de tarjetas de ambos equipos
    "rivalidad": 0.20,          # Rivalidad / importancia del partido
    "estilo_agresivo": 0.15,    # Faltas cometidas por partido
}

# Goles (Over/Under, ambos marcan)
WEIGHTS_GOALS = {
    "xg": 0.40,                 # xG a favor y en contra de ambos equipos
    "forma_goles": 0.25,        # Goles marcados/recibidos forma reciente
    "fuerza_relativa": 0.20,    # Partidos parejos → menos goles del favorito
    "bajas_ofensivas": 0.15,    # Bajas ofensivas/defensivas clave
}

# Goleador anota (anytime)
WEIGHTS_GOALSCORER = {
    "tasa_gol_90": 0.45,        # Tasa de gol por 90 min del jugador
    "minutos_esperados": 0.25,  # Titular / suplente / rotation
    "fuerza_defensiva_rival": 0.20,  # Calidad defensiva del contrario
    "penales": 0.10,            # ¿Es el lanzador de penales?
}

# Tabla consolidada — para iterar por mercado desde /patterns
ALL_WEIGHTS = {
    "match_result": WEIGHTS_RESULT,
    "corners": WEIGHTS_CORNERS,
    "cards": WEIGHTS_CARDS,
    "goals": WEIGHTS_GOALS,
    "goalscorer": WEIGHTS_GOALSCORER,
}


def get_weights(market: str) -> dict:
    """Devuelve los pesos para un mercado. Si no existe, usa WEIGHTS_RESULT."""
    return ALL_WEIGHTS.get(market, WEIGHTS_RESULT).copy()


def get_neutral_weights(market: str) -> dict:
    """
    Pesos ajustados para partidos en cancha neutral.
    Para 'match_result': zeroes out 'localía' y redistribuye su peso
    proporcionalmente entre los demás factores (la suma sigue siendo 1.0).
    Para otros mercados: igual que get_weights() (localía no aplica).
    """
    if market != "match_result":
        return get_weights(market)

    base = WEIGHTS_RESULT.copy()
    localia_w = base.pop("localía", 0.0)
    # Redistribuir proporcionalmente entre los factores restantes
    remaining_total = sum(base.values())
    if remaining_total > 0 and localia_w > 0:
        base = {k: v + (v / remaining_total) * localia_w for k, v in base.items()}
    base["localía"] = 0.0  # Mantener la clave para que el driver se muestre con señal "~"
    return base


def update_weights(market: str, new_weights: dict) -> dict:
    """
    Actualiza los pesos de un mercado en memoria.
    Normaliza para que sumen 1.0.
    Retorna los pesos normalizados.
    """
    total = sum(new_weights.values())
    if total <= 0:
        raise ValueError(f"Los pesos para '{market}' suman 0 o negativo.")
    normalized = {k: v / total for k, v in new_weights.items()}
    ALL_WEIGHTS[market] = normalized
    return normalized
