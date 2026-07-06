"""
Tests del motor de mercados con datos fixture.
"""
from app.models.markets import calculate_all_markets, suggested_combo


FIXTURE_DATA = {
    "home_team": "Argentina",
    "away_team": "Nigeria",
    "lambda_home": 2.1,
    "lambda_away": 0.9,
    "lambda_corners_home": 6.0,
    "lambda_corners_away": 4.0,
    "lambda_cards_home": 2.0,
    "lambda_cards_away": 2.0,
    "xg_home": 2.1,
    "xg_away": 0.9,
    "avg_corners": 10.0,
    "referee_cards_avg": 3.5,
    "fuerza_relativa": 0.6,
    "forma_5": 0.4,
    "bajas_clave": 0.0,
    "h2h_advantage": 0.3,
    "localía": 0.1,
    "importancia": 0.0,
    "players": [
        {"name": "Messi", "team": "home", "goals_per_90": 0.65, "expected_minutes": 90, "team_avg_goals": 2.0}
    ],
    "goals_line": 2.5,
    "corners_line": 9.5,
    "cards_line": 3.5,
}


def test_calculate_returns_legs():
    legs = calculate_all_markets(FIXTURE_DATA, include_low=False)
    assert len(legs) > 0


def test_ranks_assigned():
    legs = calculate_all_markets(FIXTURE_DATA)
    ranks = [l.rank for l in legs]
    assert ranks[0] == 1
    assert sorted(ranks) == list(range(1, len(ranks) + 1))


def test_sorted_by_prob_desc():
    legs = calculate_all_markets(FIXTURE_DATA)
    probs = [l.probability for l in legs]
    assert probs == sorted(probs, reverse=True)


def test_probabilities_valid_range():
    legs = calculate_all_markets(FIXTURE_DATA, include_low=True)
    for l in legs:
        assert 0.0 <= l.probability <= 1.0, f"{l.market} prob fuera de rango: {l.probability}"


def test_suggested_combo_math():
    legs = calculate_all_markets(FIXTURE_DATA)
    combo = suggested_combo(legs, top_n=3)
    # La probabilidad combinada debe ser <= la menor de las patas
    min_prob = min(legs[:3], key=lambda x: x.probability).probability
    assert combo["combined_probability"] <= min_prob + 0.001


def test_goalscorer_in_legs():
    legs = calculate_all_markets(FIXTURE_DATA, include_low=False)
    markets = [l.market for l in legs]
    assert "goalscorer_anytime" in markets


def test_low_confidence_excluded_by_default():
    legs = calculate_all_markets(FIXTURE_DATA, include_low=False)
    confidences = [l.confidence for l in legs]
    assert "low" not in confidences


def test_low_confidence_included_when_requested():
    legs = calculate_all_markets(FIXTURE_DATA, include_low=True)
    confidences = [l.confidence for l in legs]
    assert "low" in confidences
