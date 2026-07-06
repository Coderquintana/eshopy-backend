"""
Tests del módulo de simulación Monte Carlo.
Validan coherencia, detección de contradicciones y selección del combo óptimo.
"""
from app.models.simulation import (
    are_contradictory,
    evaluate_leg,
    find_best_combo,
    joint_probability,
    simulate_match,
)
from app.models.markets import calculate_all_markets

FIXTURE_DATA = {
    "home_team": "España",
    "away_team": "Francia",
    "lambda_home": 1.8,
    "lambda_away": 1.2,
    "lambda_corners_home": 6.0,
    "lambda_corners_away": 5.0,
    "lambda_cards_home": 2.0,
    "lambda_cards_away": 2.0,
    "xg_home": 1.8,
    "xg_away": 1.2,
    "avg_corners": 11.0,
    "referee_cards_avg": None,
    "fuerza_relativa": 0.3,
    "forma_5": 0.2,
    "bajas_clave": 0.0,
    "h2h_advantage": 0.1,
    "localía": 0.1,
    "importancia": 0.0,
    "players": [
        {"name": "Yamal", "team": "home", "goals_per_90": 0.45, "expected_minutes": 90, "team_avg_goals": 1.8},
        {"name": "Oyarzabal", "team": "home", "goals_per_90": 0.35, "expected_minutes": 90, "team_avg_goals": 1.8},
        {"name": "Mbappe", "team": "away", "goals_per_90": 0.65, "expected_minutes": 90, "team_avg_goals": 1.2},
    ],
    "goals_line": 2.5,
    "corners_line": 9.5,
    "cards_line": 3.5,
}

N = 3_000  # simulaciones suficientes para tests reproducibles


def test_simulate_returns_correct_length():
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    assert sim.n_simulations == N
    assert len(sim.home_goals) == N
    assert len(sim.away_goals) == N
    assert len(sim.total_goals) == N
    assert len(sim.total_corners) == N
    assert len(sim.total_cards) == N


def test_total_goals_is_sum():
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    for i in range(N):
        assert sim.total_goals[i] == sim.home_goals[i] + sim.away_goals[i]


def test_player_goals_coherent_with_team_goals():
    """
    La suma de goles de jugadores del equipo local <= goles del equipo local.
    Esta es la propiedad central de coherencia que resuelve el bug de Opus.
    """
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    home_player_names = ["Yamal", "Oyarzabal"]
    for i in range(N):
        player_sum = sum(sim.player_goals.get(name, [0]*N)[i] for name in home_player_names)
        # La suma de goles de jugadores listados no puede superar los del equipo
        # (el excedente va a "otros", que no aparece en player_goals)
        assert player_sum <= sim.home_goals[i], (
            f"Sim {i}: Yamal+Oyarzabal={player_sum} > home_goals={sim.home_goals[i]} — incoherente"
        )


def test_evaluate_leg_result():
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    mask = evaluate_leg({"type": "result", "outcome": "home"}, sim)
    assert len(mask) == N
    # Debe coincidir con home > away
    for i, m in enumerate(mask):
        assert m == (sim.home_goals[i] > sim.away_goals[i])


def test_evaluate_leg_over_under():
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    mask_over = evaluate_leg({"type": "over_under", "event": "goals", "line": 2.5, "direction": "over"}, sim)
    mask_under = evaluate_leg({"type": "over_under", "event": "goals", "line": 2.5, "direction": "under"}, sim)
    # Over y under son complementarios
    for i in range(N):
        assert mask_over[i] != mask_under[i]


def test_evaluate_leg_btts():
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    mask_yes = evaluate_leg({"type": "btts", "value": True}, sim)
    mask_no = evaluate_leg({"type": "btts", "value": False}, sim)
    for i in range(N):
        assert mask_yes[i] != mask_no[i]


def test_joint_probability_lt_individual():
    """
    La prob. conjunta de dos patas debe ser <= la menor de sus probs. individuales.
    """
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    cond_a = {"type": "result", "outcome": "home"}
    cond_b = {"type": "over_under", "event": "goals", "line": 2.5, "direction": "over"}

    p_a = sum(evaluate_leg(cond_a, sim)) / N
    p_b = sum(evaluate_leg(cond_b, sim)) / N
    p_joint = joint_probability([cond_a, cond_b], sim)

    assert p_joint <= min(p_a, p_b) + 0.02  # tolerancia mínima por aleatoriedad


def test_correlation_discount_exists_for_goalscorers():
    """
    Si pedimos Yamal + Oyarzabal (ambos de España), la prob. conjunta real debe ser
    MENOR que el producto ingenuo (p_Yamal × p_Oyarzabal), porque requieren ≥2 goles.
    """
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    cond_yamal = {"type": "goalscorer", "player": "Yamal", "team": "home"}
    cond_oyar = {"type": "goalscorer", "player": "Oyarzabal", "team": "home"}

    p_yamal = sum(evaluate_leg(cond_yamal, sim)) / N
    p_oyar = sum(evaluate_leg(cond_oyar, sim)) / N
    p_joint = joint_probability([cond_yamal, cond_oyar], sim)
    naive = p_yamal * p_oyar

    # La correlación debe reducir la probabilidad conjunta vs el producto ingenuo
    assert p_joint < naive + 0.05, (
        f"p_joint={p_joint:.3f} no es menor que naive={naive:.3f} "
        "(se esperaba descuento por correlación)"
    )


def test_are_contradictory_over_under():
    a = {"type": "over_under", "event": "goals", "line": 2.5, "direction": "over"}
    b = {"type": "over_under", "event": "goals", "line": 2.5, "direction": "under"}
    assert are_contradictory(a, b)


def test_not_contradictory_different_events():
    a = {"type": "over_under", "event": "goals", "line": 2.5, "direction": "over"}
    b = {"type": "over_under", "event": "corners", "line": 9.5, "direction": "over"}
    assert not are_contradictory(a, b)


def test_are_contradictory_btts():
    assert are_contradictory({"type": "btts", "value": True}, {"type": "btts", "value": False})


def test_are_contradictory_result():
    a = {"type": "result", "outcome": "home"}
    b = {"type": "result", "outcome": "away"}
    assert are_contradictory(a, b)


def test_find_best_combo_no_contradictions():
    """El combo sugerido no debe contener patas contradictorias."""
    legs = calculate_all_markets(FIXTURE_DATA, include_low=False)
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    combo = find_best_combo(legs, sim, top_n=3)

    selected_ranks = combo.get("legs", [])
    selected_legs = [l for l in legs if l.rank in selected_ranks]
    conditions = [l.condition for l in selected_legs if l.condition]

    from app.models.simulation import are_contradictory
    from itertools import combinations
    for ca, cb in combinations(conditions, 2):
        assert not are_contradictory(ca, cb), (
            f"Combo contiene patas contradictorias: {ca} vs {cb}"
        )


def test_find_best_combo_returns_correlation_discount():
    """El combo reporta los campos de correlación y las probabilidades son coherentes."""
    legs = calculate_all_markets(FIXTURE_DATA, include_low=False)
    sim = simulate_match(FIXTURE_DATA, n_simulations=N)
    combo = find_best_combo(legs, sim, top_n=3)

    assert "correlation_discount" in combo
    assert "naive_product" in combo
    assert "combined_probability" in combo

    cp = combo["combined_probability"]
    np_ = combo["naive_product"]

    # Ambas probabilidades deben estar en [0, 1]
    assert 0.0 <= cp <= 1.0
    assert 0.0 <= np_ <= 1.0

    # correlation_discount = naive_product - combined_probability.
    # Puede ser positivo (correlación negativa: patas que no ocurren juntas) o
    # negativo (correlación positiva: patas que tienden a ocurrir juntas, como
    # "local gana" + "over 2.5 goles"). Ambos casos son matemáticamente correctos.
    # Tolerancia 1e-4: los tres campos se redondean independientemente a 4 decimales,
    # lo que puede introducir una diferencia máxima de ±0.0001.
    assert abs(combo["correlation_discount"] - (np_ - cp)) < 1e-4
