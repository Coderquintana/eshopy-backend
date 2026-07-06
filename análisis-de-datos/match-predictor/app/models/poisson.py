"""
Motor probabilístico basado en Poisson bivariado (con corrección Dixon-Coles).
Implementado en Python puro para máxima portabilidad (sin numpy/scipy).

Por qué Poisson: los goles en fútbol son eventos raros e independientes entre sí.
La distribución de Poisson modela exactamente eso: cuántos eventos ocurren en un
intervalo dado, con tasa lambda conocida.

Dixon-Coles (1997) añade una corrección para partidos de muy bajo marcador (0-0, 1-0,
0-1, 1-1) que Poisson simple sobreestima/subestima.
"""
from __future__ import annotations

import math
from dataclasses import dataclass
from typing import Optional


@dataclass
class MatchLambdas:
    """Tasas de Poisson (goles esperados) para el partido."""
    lambda_home: float
    lambda_away: float
    lambda_corners_home: float = 5.5
    lambda_corners_away: float = 4.5
    lambda_cards_home: float = 2.0
    lambda_cards_away: float = 1.8


# ---------------------------------------------------------------------------
# Funciones de distribución Poisson (Python puro)
# ---------------------------------------------------------------------------

def _poisson_pmf(k: int, mu: float) -> float:
    """P(X = k) donde X ~ Poisson(mu). Usa log para evitar overflow."""
    if mu <= 0:
        return 1.0 if k == 0 else 0.0
    if k < 0:
        return 0.0
    try:
        log_p = -mu + k * math.log(mu) - math.lgamma(k + 1)
        return math.exp(log_p)
    except (ValueError, OverflowError):
        return 0.0


def _poisson_cdf(k: int, mu: float) -> float:
    """P(X <= k) donde X ~ Poisson(mu)."""
    return sum(_poisson_pmf(i, mu) for i in range(k + 1))


def _poisson_survival(k: int, mu: float) -> float:
    """P(X > k) donde X ~ Poisson(mu). = 1 - CDF(k)."""
    return max(0.0, 1.0 - _poisson_cdf(k, mu))


# ---------------------------------------------------------------------------
# Corrección Dixon-Coles para marcadores bajos
# ---------------------------------------------------------------------------

def _dixon_coles_correction(home_goals: int, away_goals: int,
                             lh: float, la: float, rho: float = -0.1) -> float:
    """
    Factor de corrección Dixon-Coles para marcadores bajos.
    rho negativo porque 0-0 y 1-1 son menos frecuentes de lo que predice Poisson puro.
    """
    if home_goals == 0 and away_goals == 0:
        return 1 - lh * la * rho
    elif home_goals == 1 and away_goals == 0:
        return 1 + la * rho
    elif home_goals == 0 and away_goals == 1:
        return 1 + lh * rho
    elif home_goals == 1 and away_goals == 1:
        return 1 - rho
    return 1.0


# ---------------------------------------------------------------------------
# Matriz de marcadores
# ---------------------------------------------------------------------------

def score_matrix(lh: float, la: float, max_goals: int = 10,
                 rho: float = -0.1) -> list[list[float]]:
    """
    Matriz [home_goals][away_goals] con la probabilidad de cada marcador exacto.
    Retorna lista de listas (Python puro, sin numpy).
    """
    matrix = []
    for h in range(max_goals + 1):
        row = []
        for a in range(max_goals + 1):
            p = _poisson_pmf(h, lh) * _poisson_pmf(a, la)
            p *= _dixon_coles_correction(h, a, lh, la, rho)
            row.append(p)
        matrix.append(row)

    # Normalizar para que sume exactamente 1
    total = sum(cell for row in matrix for cell in row)
    if total > 0:
        matrix = [[cell / total for cell in row] for row in matrix]
    return matrix


# ---------------------------------------------------------------------------
# Probabilidades principales
# ---------------------------------------------------------------------------

def prob_result(lh: float, la: float) -> dict[str, float]:
    """Probabilidades de victoria local, empate y victoria visitante."""
    mat = score_matrix(lh, la)
    size = len(mat)
    p_home = sum(mat[h][a] for h in range(size) for a in range(size) if h > a)
    p_draw = sum(mat[h][h] for h in range(size))
    p_away = sum(mat[h][a] for h in range(size) for a in range(size) if a > h)
    total = p_home + p_draw + p_away
    if total > 0:
        p_home /= total
        p_draw /= total
        p_away /= total
    return {"home": p_home, "draw": p_draw, "away": p_away}


def prob_goals_over_under(lh: float, la: float, line: float) -> dict[str, float]:
    """Probabilidad de que los goles totales sean Over/Under una línea."""
    mat = score_matrix(lh, la)
    size = len(mat)
    over = sum(
        mat[h][a]
        for h in range(size)
        for a in range(size)
        if h + a > line
    )
    over = min(over, 1.0)
    return {"over": over, "under": max(0.0, 1.0 - over)}


def prob_both_score(lh: float, la: float) -> float:
    """Probabilidad de que ambos equipos marquen al menos un gol (BTTS)."""
    p_home_0 = _poisson_pmf(0, lh)
    p_away_0 = _poisson_pmf(0, la)
    return 1.0 - p_home_0 - p_away_0 + p_home_0 * p_away_0


def prob_corners_over_under(lambda_corners: float, line: float) -> dict[str, float]:
    """Over/Under en córners totales."""
    k_floor = int(line)
    over = float(_poisson_survival(k_floor, lambda_corners))
    return {"over": over, "under": max(0.0, 1.0 - over)}


def prob_cards_over_under(lambda_cards: float, line: float) -> dict[str, float]:
    """Over/Under en tarjetas totales."""
    k_floor = int(line)
    over = float(_poisson_survival(k_floor, lambda_cards))
    return {"over": over, "under": max(0.0, 1.0 - over)}


def prob_goalscorer(goals_per_90: float, expected_minutes: float,
                    team_goals_expected: float,
                    team_avg_goals: float = 1.5) -> float:
    """
    Probabilidad de que un jugador específico anote (anytime goalscorer).
    P(al menos 1 gol) = 1 - P(0 goles).
    """
    if expected_minutes <= 0:
        return 0.0
    match_factor = team_goals_expected / max(team_avg_goals, 0.1)
    player_lambda = (goals_per_90 / 90.0) * expected_minutes * match_factor
    return float(1.0 - _poisson_pmf(0, player_lambda))


def estimate_lambdas_from_stats(
    home_goals_scored_avg: float,
    home_goals_conceded_avg: float,
    away_goals_scored_avg: float,
    away_goals_conceded_avg: float,
    league_avg_goals: float = 1.3,
    home_advantage: float = 1.2,
) -> tuple[float, float]:
    """
    Estimación de lambdas usando fuerza de ataque/defensa (método Dixon-Coles).
    home_advantage = 1.2 empíricamente en partidos de liga.
    Usar home_advantage=1.0 cuando el partido se juega en campo neutral.
    """
    league_avg = max(league_avg_goals, 0.01)
    attack_h = home_goals_scored_avg / league_avg
    defense_h = home_goals_conceded_avg / league_avg
    attack_a = away_goals_scored_avg / league_avg
    defense_a = away_goals_conceded_avg / league_avg

    lambda_home = attack_h * defense_a * league_avg * home_advantage
    lambda_away = attack_a * defense_h * league_avg

    # Cap defensivo: >3 goles esperados por partido es estadísticamente poco realista.
    # La regularización por shrinkage debería resolver la mayoría de los casos;
    # este cap es la red de seguridad final.
    lambda_home = max(0.1, min(lambda_home, 3.0))
    lambda_away = max(0.1, min(lambda_away, 3.0))
    return lambda_home, lambda_away
