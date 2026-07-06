"""
Tests del motor Poisson con datos fixture (sin llamadas HTTP).
Ejecutar: python -m pytest tests/
"""
import pytest
from app.models.poisson import (
    estimate_lambdas_from_stats,
    prob_both_score,
    prob_goals_over_under,
    prob_result,
    score_matrix,
)


def test_score_matrix_sums_to_one():
    mat = score_matrix(1.5, 1.2)
    total = sum(cell for row in mat for cell in row)
    assert abs(total - 1.0) < 0.001


def test_prob_result_sums_to_one():
    probs = prob_result(1.5, 1.2)
    total = probs["home"] + probs["draw"] + probs["away"]
    assert abs(total - 1.0) < 0.001


def test_strong_home_favored():
    # Equipo local muy superior → prob local > prob visitante
    probs = prob_result(3.0, 0.5)
    assert probs["home"] > probs["away"]


def test_over_under_complement():
    ou = prob_goals_over_under(1.5, 1.2, 2.5)
    assert abs(ou["over"] + ou["under"] - 1.0) < 0.001


def test_btts_range():
    prob = prob_both_score(1.3, 1.0)
    assert 0.0 <= prob <= 1.0


def test_estimate_lambdas_reasonable():
    lh, la = estimate_lambdas_from_stats(1.8, 1.0, 1.0, 1.5, 1.3)
    assert 0.1 <= lh <= 5.0
    assert 0.1 <= la <= 5.0


def test_equal_teams_balanced():
    probs = prob_result(1.3, 1.3)
    # Con equipos iguales en cancha neutral, home leve ventaja por el +1.2
    assert abs(probs["home"] - probs["away"]) < 0.15
