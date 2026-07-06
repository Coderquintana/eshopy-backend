"""
Capa Flask: rutas, validación de request, serialización.
Esta capa no contiene lógica de negocio; delega todo a models/ y context/.
"""
from __future__ import annotations

import uuid
from datetime import datetime, timezone

from flask import Blueprint, jsonify, request

from app.context.news import ContextEnricher
from app.context.value_alert import process_all_alerts
from app.core import cache as cache_module
from app.core.logger import get_logger
from app.models.markets import calculate_all_markets
from app.models.poisson import estimate_lambdas_from_stats
from app.models.simulation import find_best_combo, simulate_match
from app.providers.api_football import ApiFootballProvider
from app.providers.football_data import FootballDataProvider
from app.providers.soccerdata_src import SoccerDataProvider
from app.storage import db
from app.storage.models_orm import Outcome, Prediction, WeightVersion
from app.models.weights import ALL_WEIGHTS, update_weights

log = get_logger(__name__)
bp = Blueprint("api", __name__)

# Instancias de proveedores
_api_football = ApiFootballProvider()
_football_data = FootballDataProvider()
_soccerdata = SoccerDataProvider()
_enricher = ContextEnricher()


# ---------------------------------------------------------------------------
# POST /predict
# ---------------------------------------------------------------------------

@bp.route("/predict", methods=["POST"])
def predict():
    """
    Predice las probabilidades de múltiples mercados para un partido.
    ---
    tags:
      - Predicción
    consumes:
      - application/json
    parameters:
      - in: body
        name: body
        required: true
        schema:
          id: PredictRequest
          required:
            - home_team
            - away_team
          properties:
            home_team:
              type: string
              example: Argentina
            away_team:
              type: string
              example: Nigeria
            competition:
              type: string
              example: World Cup
            date:
              type: string
              example: "2026-06-20"
            top_n:
              type: integer
              example: 6
              default: 6
            include_low_confidence:
              type: boolean
              example: false
              default: false
            goals_line:
              type: number
              example: 2.5
            corners_line:
              type: number
              example: 9.5
            cards_line:
              type: number
              example: 3.5
            neutral_venue:
              type: boolean
              example: false
              default: false
              description: >
                Indica que el partido se juega en cancha neutral (sin ventaja de local).
                true anula el factor localía y recalcula lambdas sin home_advantage.
                El proveedor intenta auto-detectarlo; este parámetro tiene prioridad.
            manual_stats:
              type: object
              description: >
                Sobreescribe stats del modelo directamente. Útil cuando no hay API key
                o para backtests históricos con datos propios.
                Campos válidos: lambda_home, lambda_away, xg_home, xg_away,
                lambda_corners_home, lambda_corners_away, lambda_cards_home,
                lambda_cards_away, fuerza_relativa, forma_5, bajas_clave,
                h2h_advantage, avg_corners, referee_cards_avg.
              example:
                lambda_home: 1.5
                lambda_away: 1.2
                fuerza_relativa: 0.1
                avg_corners: 11.5
            manual_context:
              type: object
              properties:
                injuries:
                  type: array
                  items:
                    type: string
                notes:
                  type: string
    responses:
      200:
        description: Predicción generada con éxito
      400:
        description: Request inválido
      500:
        description: Error interno
    """
    payload = request.get_json(silent=True)
    if not payload:
        return jsonify({"error": "Body JSON requerido"}), 400

    home_team = payload.get("home_team", "").strip()
    away_team = payload.get("away_team", "").strip()
    if not home_team or not away_team:
        return jsonify({"error": "home_team y away_team son requeridos"}), 400

    competition = payload.get("competition", "")
    date = payload.get("date", datetime.now(timezone.utc).strftime("%Y-%m-%d"))
    top_n = int(payload.get("top_n", 6))
    include_low = bool(payload.get("include_low_confidence", False))
    manual_context = payload.get("manual_context")

    # 1. Obtener datos del proveedor principal
    match_data = _api_football.get_match_data(home_team, away_team, competition, date)

    # 2. Aplicar líneas personalizadas si vienen en el request
    if match_data:
        if "goals_line" in payload:
            match_data.goals_line = float(payload["goals_line"])
        if "corners_line" in payload:
            match_data.corners_line = float(payload["corners_line"])
        if "cards_line" in payload:
            match_data.cards_line = float(payload["cards_line"])

    # 2b. Resolver campo neutral: request > auto-detección del proveedor
    neutral_venue_req = payload.get("neutral_venue")  # None = no especificado
    neutral = _resolve_neutral_venue(neutral_venue_req, match_data)
    _apply_neutral_venue(neutral, match_data)

    # 3. Enriquecer con contexto cualitativo
    signals = _enricher.enrich(manual_context)
    adjustments = signals.to_weight_adjustments()

    if match_data:
        match_data.lambda_home *= adjustments["lambda_home_factor"]
        match_data.lambda_away *= adjustments["lambda_away_factor"]
        match_data.lambda_cards_home *= adjustments["cards_home_factor"]
        match_data.lambda_cards_away *= adjustments["cards_away_factor"]

    # 4. Calcular mercados
    model_dict = match_data.to_model_dict() if match_data else _default_model_dict(home_team, away_team, neutral_venue=neutral)
    # Sobreescritura de stats manuales (para backtests sin API key o calibración propia)
    manual_stats_overrides = _apply_manual_stats(model_dict, payload.get("manual_stats") or {})
    legs = calculate_all_markets(model_dict, include_low=include_low)

    # 5. Alertas de valor vs cuota de casa
    odds = match_data.odds if match_data else {}
    forma_5 = match_data.forma_5 if match_data else 0.0
    value_alerts = process_all_alerts(legs, odds, forma_5=forma_5)

    # 6. Combo sugerido via Monte Carlo (incorpora correlaciones entre patas)
    sim = simulate_match(model_dict)
    combo = find_best_combo(legs, sim, top_n=min(3, top_n), include_low=include_low)

    # 7. Data quality
    data_quality = {
        "sources_used": match_data.sources_used if match_data else [],
        "missing": match_data.missing if match_data else ["all_data"],
        "warnings": match_data.warnings if match_data else ["No se obtuvieron datos del proveedor; usando valores por defecto"],
    }
    if manual_stats_overrides:
        data_quality["manual_stats_applied"] = manual_stats_overrides
    if manual_context:
        data_quality["context_enrichment"] = adjustments

    # 8. Persistir predicción
    prediction_id = str(uuid.uuid4())
    legs_ranked_dicts = [l.to_dict() for l in legs[:top_n]]

    try:
        session = db.get_session()
        pred = Prediction(
            id=prediction_id,
            home_team=home_team,
            away_team=away_team,
            competition=competition,
            match_date=date,
            request_payload=payload,
            legs_ranked=legs_ranked_dicts,
            suggested_combo=combo,
            value_alerts=value_alerts,
            data_quality=data_quality,
        )
        session.add(pred)
        session.commit()
        session.close()
        log.info("Predicción guardada: %s (%s vs %s)", prediction_id, home_team, away_team)
    except Exception as exc:
        log.error("Error guardando predicción: %s", exc)

    return jsonify({
        "prediction_id": prediction_id,
        "match": {"home": home_team, "away": away_team, "competition": competition, "date": date,
                  "neutral_venue": neutral},
        "data_quality": data_quality,
        "legs_ranked": legs_ranked_dicts,
        "value_alerts": value_alerts,
        "suggested_combo": combo,
    })


# ---------------------------------------------------------------------------
# POST /results/<prediction_id>
# ---------------------------------------------------------------------------

@bp.route("/results/<prediction_id>", methods=["POST"])
def record_result(prediction_id: str):
    """
    Registra el resultado real del partido. Alimenta el bucle de aprendizaje.
    ---
    tags:
      - Aprendizaje
    parameters:
      - in: path
        name: prediction_id
        type: string
        required: true
      - in: body
        name: body
        required: true
        schema:
          id: ResultRequest
          properties:
            outcomes:
              type: object
              example: {"match_result": true, "corners_over": false}
            final_score:
              type: string
              example: "3-1"
            actual_corners:
              type: integer
              example: 7
            actual_cards:
              type: integer
              example: 4
    responses:
      200:
        description: Resultado registrado
      404:
        description: Predicción no encontrada
    """
    payload = request.get_json(silent=True) or {}

    session = db.get_session()
    pred = session.get(Prediction, prediction_id)
    if not pred:
        session.close()
        return jsonify({"error": f"Predicción '{prediction_id}' no encontrada"}), 404

    outcomes_raw = payload.get("outcomes", {})
    # Calcular aciertos del combo
    combo = pred.suggested_combo or {}
    combo_legs_ranks = combo.get("legs", [])
    legs_ranked = pred.legs_ranked or []
    hits = 0
    for rank in combo_legs_ranks:
        leg = next((l for l in legs_ranked if l.get("rank") == rank), None)
        if leg:
            market = leg.get("market", "")
            if market in outcomes_raw and outcomes_raw[market]:
                hits += 1

    outcome = Outcome(
        prediction_id=prediction_id,
        outcomes=outcomes_raw,
        final_score=payload.get("final_score"),
        actual_corners=payload.get("actual_corners"),
        actual_cards=payload.get("actual_cards"),
        combo_hits=hits,
        combo_total=len(combo_legs_ranks),
    )
    pred.result_recorded = True
    session.add(outcome)
    session.commit()
    session.close()

    log.info("Resultado registrado para predicción %s", prediction_id)
    return jsonify({
        "prediction_id": prediction_id,
        "recorded": True,
        "combo_accuracy": f"{hits}/{len(combo_legs_ranks)} patas del combo acertaron",
    })


# ---------------------------------------------------------------------------
# GET /patterns
# ---------------------------------------------------------------------------

@bp.route("/patterns", methods=["GET"])
def patterns():
    """
    Análisis agregado: tasa de acierto por mercado, calibración, sesgos.
    Este es el corazón del ejercicio de aprendizaje.
    ---
    tags:
      - Aprendizaje
    responses:
      200:
        description: Patrones estadísticos calculados
    """
    session = db.get_session()
    predictions = session.query(Prediction).filter(Prediction.result_recorded == True).all()
    outcomes = session.query(Outcome).all()
    session.close()

    outcomes_by_id = {o.prediction_id: o for o in outcomes}

    # Acumuladores por mercado
    market_stats: dict[str, dict] = {}

    for pred in predictions:
        outcome = outcomes_by_id.get(pred.id)
        if not outcome:
            continue
        legs = pred.legs_ranked or []
        outcome_map = outcome.outcomes or {}

        for leg in legs:
            market = leg.get("market", "unknown")
            prob = leg.get("probability", 0)
            sel = leg.get("selection", "")

            # Determinar si esta pata acertó
            hit = _check_leg_outcome(
                leg, outcome_map, outcome,
                pred.home_team or "", pred.away_team or "",
            )
            if hit is None:
                continue

            if market not in market_stats:
                market_stats[market] = {
                    "market": market,
                    "total": 0,
                    "hits": 0,
                    "prob_buckets": {},  # calibración: agrupar por prob redondeada a 0.1
                }
            stats = market_stats[market]
            stats["total"] += 1
            if hit:
                stats["hits"] += 1

            # Calibración: agrupar predicciones por rango de probabilidad
            bucket = round(round(prob * 10) / 10, 1)
            if bucket not in stats["prob_buckets"]:
                stats["prob_buckets"][bucket] = {"predicted": bucket, "total": 0, "hits": 0}
            stats["prob_buckets"][bucket]["total"] += 1
            if hit:
                stats["prob_buckets"][bucket]["hits"] += 1

    # Construir respuesta
    result = []
    for market, stats in market_stats.items():
        total = stats["total"]
        hits = stats["hits"]
        accuracy = hits / total if total > 0 else None
        calibration = []
        for bucket, bdata in sorted(stats["prob_buckets"].items()):
            bt = bdata["total"]
            bh = bdata["hits"]
            actual_rate = bh / bt if bt > 0 else None
            calibration.append({
                "predicted_prob": bucket,
                "actual_rate": round(actual_rate, 3) if actual_rate is not None else None,
                "total_samples": bt,
                "calibration_error": round(abs(bucket - actual_rate), 3) if actual_rate is not None else None,
            })
        result.append({
            "market": market,
            "total_predictions": total,
            "total_hits": hits,
            "accuracy": round(accuracy, 3) if accuracy is not None else None,
            "calibration": calibration,
        })

    # Ordenar por volumen de predicciones
    result.sort(key=lambda x: x["total_predictions"], reverse=True)

    return jsonify({
        "total_predictions_with_results": len(predictions),
        "markets": result,
        "note": (
            "La calibración es la métrica estrella: si dices 60% y aciertas el 60% de las veces, "
            "el modelo es honesto. Calibration_error cercano a 0 = buen modelo."
        ),
    })


# ---------------------------------------------------------------------------
# GET /health
# ---------------------------------------------------------------------------

@bp.route("/health", methods=["GET"])
def health():
    """
    Estado del sistema: proveedores, cache y base de datos.
    ---
    tags:
      - Sistema
    responses:
      200:
        description: Estado del sistema
    """
    providers_status = [
        _api_football.health_check(),
        _football_data.health_check(),
        _soccerdata.health_check(),
    ]
    cache_stats = cache_module.stats()

    session = db.get_session()
    total_predictions = session.query(Prediction).count()
    total_with_results = session.query(Prediction).filter(Prediction.result_recorded == True).count()
    session.close()

    return jsonify({
        "status": "ok",
        "providers": providers_status,
        "cache": cache_stats,
        "database": {
            "total_predictions": total_predictions,
            "predictions_with_results": total_with_results,
        },
    })


# ---------------------------------------------------------------------------
# GET /predictions  (bonus: listar predicciones)
# ---------------------------------------------------------------------------

@bp.route("/predictions", methods=["GET"])
def list_predictions():
    """
    Lista las últimas predicciones guardadas.
    ---
    tags:
      - Predicción
    parameters:
      - in: query
        name: limit
        type: integer
        default: 20
    responses:
      200:
        description: Lista de predicciones
    """
    limit = int(request.args.get("limit", 20))
    session = db.get_session()
    preds = (
        session.query(Prediction)
        .order_by(Prediction.created_at.desc())
        .limit(limit)
        .all()
    )
    session.close()
    return jsonify([
        {
            "prediction_id": p.id,
            "home_team": p.home_team,
            "away_team": p.away_team,
            "competition": p.competition,
            "match_date": p.match_date,
            "created_at": p.created_at.isoformat() if p.created_at else None,
            "result_recorded": p.result_recorded,
        }
        for p in preds
    ])


# ---------------------------------------------------------------------------
# POST /backtest
# ---------------------------------------------------------------------------

@bp.route("/backtest", methods=["POST"])
def backtest():
    """
    Ejecuta una predicción retroactiva sobre un partido ya jugado y registra el
    resultado automáticamente. Permite calibrar el modelo con datos históricos
    antes de acumular predicciones en vivo.

    Flujo:
      1. Corre el pipeline de predicción igual que POST /predict.
      2. Guarda la predicción con flag backtest=True.
      3. Registra el outcome conocido automáticamente.
      4. Retorna predicción + análisis de aciertos inmediato.
    ---
    tags:
      - Aprendizaje
    consumes:
      - application/json
    parameters:
      - in: body
        name: body
        required: true
        schema:
          id: BacktestRequest
          required:
            - home_team
            - away_team
            - known_result
          properties:
            home_team:
              type: string
              example: Barcelona
            away_team:
              type: string
              example: Real Madrid
            competition:
              type: string
              example: La Liga
            date:
              type: string
              example: "2024-10-26"
            top_n:
              type: integer
              default: 6
            include_low_confidence:
              type: boolean
              default: false
            manual_context:
              type: object
            neutral_venue:
              type: boolean
              example: false
              default: false
              description: Cancha neutral. Prioridad sobre auto-detección del proveedor.
            manual_stats:
              type: object
              description: Sobreescribe stats del modelo (mismo esquema que en /predict).
              example:
                lambda_home: 1.4
                lambda_away: 1.3
                avg_corners: 10.5
            known_result:
              type: object
              required:
                - final_score
              properties:
                final_score:
                  type: string
                  example: "4-0"
                actual_corners:
                  type: integer
                  example: 6
                actual_cards:
                  type: integer
                  example: 5
    responses:
      200:
        description: Backtest ejecutado y resultado registrado
      400:
        description: Request inválido
    """
    payload = request.get_json(silent=True)
    if not payload:
        return jsonify({"error": "Body JSON requerido"}), 400

    known_result = payload.get("known_result")
    if not known_result or "final_score" not in known_result:
        return jsonify({"error": "known_result.final_score es requerido"}), 400

    home_team = payload.get("home_team", "").strip()
    away_team = payload.get("away_team", "").strip()
    if not home_team or not away_team:
        return jsonify({"error": "home_team y away_team son requeridos"}), 400

    competition = payload.get("competition", "")
    date = payload.get("date", "")
    top_n = int(payload.get("top_n", 6))
    include_low = bool(payload.get("include_low_confidence", False))
    manual_context = payload.get("manual_context")

    # 1. Obtener datos y correr predicción (mismo pipeline que /predict)
    match_data = _api_football.get_match_data(home_team, away_team, competition, date)

    if match_data:
        if "goals_line" in payload:
            match_data.goals_line = float(payload["goals_line"])
        if "corners_line" in payload:
            match_data.corners_line = float(payload["corners_line"])
        if "cards_line" in payload:
            match_data.cards_line = float(payload["cards_line"])

    # Resolver campo neutral (misma lógica que /predict)
    neutral_venue_req = payload.get("neutral_venue")
    neutral = _resolve_neutral_venue(neutral_venue_req, match_data)
    _apply_neutral_venue(neutral, match_data)

    signals = _enricher.enrich(manual_context)
    adjustments = signals.to_weight_adjustments()
    if match_data:
        match_data.lambda_home *= adjustments["lambda_home_factor"]
        match_data.lambda_away *= adjustments["lambda_away_factor"]

    model_dict = match_data.to_model_dict() if match_data else _default_model_dict(home_team, away_team, neutral_venue=neutral)
    manual_stats_overrides = _apply_manual_stats(model_dict, payload.get("manual_stats") or {})
    legs = calculate_all_markets(model_dict, include_low=include_low)

    odds = match_data.odds if match_data else {}
    forma_5 = match_data.forma_5 if match_data else 0.0
    value_alerts = process_all_alerts(legs, odds, forma_5=forma_5)

    sim = simulate_match(model_dict)
    combo = find_best_combo(legs, sim, top_n=min(3, top_n), include_low=include_low)

    data_quality = {
        "sources_used": match_data.sources_used if match_data else [],
        "missing": match_data.missing if match_data else ["all_data"],
        "warnings": match_data.warnings if match_data else [],
        "backtest": True,
    }
    if manual_stats_overrides:
        data_quality["manual_stats_applied"] = manual_stats_overrides

    prediction_id = str(uuid.uuid4())
    legs_ranked_dicts = [l.to_dict() for l in legs[:top_n]]

    # 2. Derivar outcomes del resultado conocido
    final_score = known_result["final_score"]
    outcomes_map = _derive_outcomes_from_score(final_score, legs_ranked_dicts, known_result)

    # 3. Calcular aciertos del combo
    combo_hits, combo_total = _score_combo(
        combo, legs_ranked_dicts, outcomes_map, known_result, home_team, away_team
    )

    # 4. Persistir predicción + outcome en una transacción
    try:
        session = db.get_session()
        pred = Prediction(
            id=prediction_id,
            home_team=home_team,
            away_team=away_team,
            competition=competition,
            match_date=date,
            request_payload={**payload, "_backtest": True},
            legs_ranked=legs_ranked_dicts,
            suggested_combo=combo,
            value_alerts=value_alerts,
            data_quality=data_quality,
            result_recorded=True,
        )
        outcome_row = Outcome(
            prediction_id=prediction_id,
            outcomes=outcomes_map,
            final_score=final_score,
            actual_corners=known_result.get("actual_corners"),
            actual_cards=known_result.get("actual_cards"),
            combo_hits=combo_hits,
            combo_total=combo_total,
        )
        session.add(pred)
        session.add(outcome_row)
        session.commit()
        session.close()
        log.info("Backtest guardado: %s (%s vs %s, resultado %s)", prediction_id, home_team, away_team, final_score)
    except Exception as exc:
        log.error("Error guardando backtest: %s", exc)

    # 5. Análisis de aciertos por pata
    hits_detail = []
    for leg in legs_ranked_dicts:
        hit = _check_leg_outcome(leg, outcomes_map, _OutcomeProxy(known_result), home_team, away_team)
        if hit is not None:
            hits_detail.append({
                "rank": leg["rank"],
                "market": leg["market"],
                "selection": leg["selection"],
                "predicted_prob": leg["probability"],
                "hit": hit,
            })

    total_evaluable = len(hits_detail)
    total_hits = sum(1 for h in hits_detail if h["hit"])

    return jsonify({
        "prediction_id": prediction_id,
        "backtest": True,
        "match": {"home": home_team, "away": away_team, "competition": competition, "date": date,
                  "neutral_venue": neutral},
        "known_result": known_result,
        "data_quality": data_quality,
        "legs_ranked": legs_ranked_dicts,
        "value_alerts": value_alerts,
        "suggested_combo": combo,
        "accuracy": {
            "total_evaluable_legs": total_evaluable,
            "hits": total_hits,
            "accuracy_rate": round(total_hits / total_evaluable, 3) if total_evaluable > 0 else None,
            "combo_hits": f"{combo_hits}/{combo_total}",
            "detail": hits_detail,
        },
    })


# ---------------------------------------------------------------------------
# POST /cache/clear  (utilidad)
# ---------------------------------------------------------------------------

@bp.route("/cache/clear", methods=["POST"])
def clear_cache():
    """
    Limpia el cache en disco.
    ---
    tags:
      - Sistema
    responses:
      200:
        description: Cache limpiado
    """
    count = cache_module.clear_all()
    return jsonify({"cleared_entries": count})


# ---------------------------------------------------------------------------
# Helpers internos
# ---------------------------------------------------------------------------

# Campos del model_dict que pueden sobreescribirse via manual_stats
_OVERRIDABLE_STATS = frozenset({
    "lambda_home", "lambda_away",
    "xg_home", "xg_away",
    "lambda_corners_home", "lambda_corners_away",
    "lambda_cards_home", "lambda_cards_away",
    "fuerza_relativa", "forma_5", "bajas_clave",
    "h2h_advantage", "localía", "importancia",
    "avg_corners", "referee_cards_avg",
})


def _apply_manual_stats(model_dict: dict, manual_stats: dict) -> list[str]:
    """
    Sobreescribe campos del model_dict con los valores de manual_stats.
    Retorna lista de campos efectivamente sobreescritos (para data_quality).
    """
    overridden = []
    for field, value in manual_stats.items():
        if field in _OVERRIDABLE_STATS and value is not None:
            try:
                model_dict[field] = float(value)
                overridden.append(field)
            except (TypeError, ValueError):
                pass
    return overridden


def _resolve_neutral_venue(request_value, match_data) -> bool:
    """
    Resuelve si el partido es en cancha neutral.
    Prioridad: parámetro del request > auto-detección del proveedor.
    """
    if request_value is not None:
        return bool(request_value)
    return bool(match_data.is_neutral_venue) if match_data else False


def _apply_neutral_venue(neutral: bool, match_data) -> None:
    """
    Aplica (o revierte) campo neutral sobre match_data en el lugar.
    Recalcula lambdas con home_advantage=1.0 (neutral) o 1.2 (no neutral)
    solo cuando el estado cambia respecto a lo que detectó el proveedor.
    """
    if match_data is None:
        return
    if neutral == match_data.is_neutral_venue:
        return  # El proveedor ya calculó correctamente; nada que hacer
    match_data.is_neutral_venue = neutral
    match_data.lambda_home, match_data.lambda_away = estimate_lambdas_from_stats(
        match_data.home_goals_scored_avg,
        match_data.home_goals_conceded_avg,
        match_data.away_goals_scored_avg,
        match_data.away_goals_conceded_avg,
        match_data.league_avg_goals,
        home_advantage=1.0 if neutral else 1.2,
    )


def _default_model_dict(home_team: str, away_team: str, neutral_venue: bool = False) -> dict:
    """Dict de modelo con valores por defecto cuando no hay datos del proveedor."""
    return {
        "home_team": home_team,
        "away_team": away_team,
        "lambda_home": 1.3,
        "lambda_away": 1.0,
        "lambda_corners_home": 5.5,
        "lambda_corners_away": 4.5,
        "lambda_cards_home": 2.0,
        "lambda_cards_away": 1.8,
        "xg_home": 1.3,
        "xg_away": 1.0,
        "avg_corners": 10.0,
        "referee_cards_avg": None,
        "fuerza_relativa": 0.0,
        "forma_5": 0.0,
        "bajas_clave": 0.0,
        "h2h_advantage": 0.0,
        "localía": 0.0 if neutral_venue else 0.1,
        "importancia": 0.0,
        "neutral_venue": neutral_venue,
        "players": [],
        "goals_line": 2.5,
        "corners_line": 9.5,
        "cards_line": 3.5,
    }


def _check_leg_outcome(
    leg: dict,
    outcome_map: dict,
    outcome: Outcome,
    home_team: str = "",
    away_team: str = "",
) -> bool | None:
    """
    Determina si una pata acertó según el outcome registrado.
    Retorna True/False/None (None = no determinable desde el marcador final).

    home_team / away_team: nombres de los equipos del partido; permiten resolver
    qué lado ganó en selecciones "Gana X" sin depender de keywords "local"/"home".
    """
    market = leg.get("market", "")
    sel = leg.get("selection", "").lower()

    # ── helper: extraer score (h, a) del final_score ─────────────────────────
    def _parse_score() -> tuple[int, int] | None:
        score = (outcome.final_score or "").strip()
        if "-" not in score:
            return None
        try:
            h, a = map(int, score.split("-", 1))
            return h, a
        except ValueError:
            return None

    # ── helper: extraer línea numérica de la selección ───────────────────────
    def _extract_line() -> float | None:
        for part in sel.split():
            try:
                return float(part)
            except ValueError:
                continue
        return None

    # ── match_result ─────────────────────────────────────────────────────────
    if market == "match_result":
        # Necesitamos que match_result esté registrado para saber que hubo resultado
        if outcome_map.get("match_result") is None:
            return None
        parsed = _parse_score()
        if parsed is None:
            return None
        h, a = parsed

        if "empate" in sel:
            return h == a

        if "gana" in sel:
            # Extraer el nombre del equipo: "gana england" → "england"
            team_in_sel = sel.replace("gana", "").strip()
            ht = home_team.lower().strip()
            at = away_team.lower().strip()
            if ht and (team_in_sel in ht or ht in team_in_sel):
                return h > a   # victoria del equipo local
            if at and (team_in_sel in at or at in team_in_sel):
                return a > h   # victoria del equipo visitante
            # Fallback cuando no se dispone de nombres: keywords explícitos
            if "local" in sel or "home" in sel:
                return h > a
            # Si no hay forma de determinar el lado, no evaluar
            return None

        return None

    # ── double_chance ─────────────────────────────────────────────────────────
    elif market == "double_chance":
        if outcome_map.get("match_result") is None:
            return None
        parsed = _parse_score()
        if parsed is None:
            return None
        h, a = parsed
        # Los patrones son "Equipo o Empate (1X)" y "Equipo o Empate (X2)"
        if "1x" in sel:
            return h >= a   # local gana O empate
        if "x2" in sel:
            return a >= h   # visitante gana O empate
        return None

    # ── goals_over_under ─────────────────────────────────────────────────────
    elif market == "goals_over_under":
        parsed = _parse_score()
        if parsed is None:
            return None
        h, a = parsed
        total = h + a
        line = _extract_line()
        if line is None:
            return None
        if "más de" in sel or "over" in sel:
            return total > line
        if "menos de" in sel or "under" in sel:
            return not (total > line)
        return None

    # ── btts (ambos marcan) ───────────────────────────────────────────────────
    elif market == "btts":
        parsed = _parse_score()
        if parsed is None:
            return None
        h, a = parsed
        both = h > 0 and a > 0
        # Selecciones: "Ambos marcan - Sí" / "Ambos marcan - No"
        if "no" in sel:
            return not both
        # Cualquier variante afirmativa ("sí", "si", "yes", o simplemente sin "no")
        return both

    # ── corners_over_under ────────────────────────────────────────────────────
    elif market == "corners_over_under":
        actual = outcome.actual_corners
        if actual is None:
            return None
        line = _extract_line()
        if line is None:
            return None
        if "más de" in sel or "over" in sel:
            return actual > line
        if "menos de" in sel or "under" in sel:
            return not (actual > line)
        return None

    # ── cards_over_under ──────────────────────────────────────────────────────
    elif market == "cards_over_under":
        actual = outcome.actual_cards
        if actual is None:
            return None
        line = _extract_line()
        if line is None:
            return None
        if "más de" in sel or "over" in sel:
            return actual > line
        if "menos de" in sel or "under" in sel:
            return not (actual > line)
        return None

    # ── first_to_score ────────────────────────────────────────────────────────
    elif market == "first_to_score":
        return None  # No determinable solo del marcador final

    return None


class _OutcomeProxy:
    """
    Proxy liviano que imita la interfaz del ORM Outcome para que _check_leg_outcome
    funcione con datos crudos del known_result de /backtest, sin tocar la DB.
    """
    def __init__(self, known_result: dict):
        self.final_score = known_result.get("final_score")
        self.actual_corners = known_result.get("actual_corners")
        self.actual_cards = known_result.get("actual_cards")


def _derive_outcomes_from_score(final_score: str, legs: list[dict], known_result: dict) -> dict:
    """
    Deriva automáticamente un mapa de outcomes {market: bool} a partir del
    marcador conocido y los datos adicionales (actual_corners, actual_cards).
    """
    outcomes: dict = {}
    proxy = _OutcomeProxy(known_result)

    for leg in legs:
        key = f"{leg.get('market', '')}_{leg.get('rank', '')}"
        result = _check_leg_outcome(leg, {}, proxy)
        if result is not None:
            outcomes[key] = result

    # Guardar resultado de partido como clave simple para /patterns
    if "-" in (final_score or ""):
        try:
            h, a = map(int, final_score.split("-"))
            outcomes["match_result"] = "home" if h > a else ("away" if a > h else "draw")
            outcomes["goals_total"] = h + a
        except ValueError:
            pass

    return outcomes


def _score_combo(
    combo: dict,
    legs: list[dict],
    outcomes_map: dict,
    known_result: dict,
    home_team: str = "",
    away_team: str = "",
) -> tuple[int, int]:
    """Calcula cuántas patas del combo sugerido acertaron. Retorna (hits, total)."""
    combo_ranks = combo.get("legs", [])
    if not combo_ranks:
        return 0, 0
    proxy = _OutcomeProxy(known_result)
    hits = 0
    for rank in combo_ranks:
        leg = next((l for l in legs if l.get("rank") == rank), None)
        if leg is None:
            continue
        result = _check_leg_outcome(leg, outcomes_map, proxy, home_team, away_team)
        if result:
            hits += 1
    return hits, len(combo_ranks)
