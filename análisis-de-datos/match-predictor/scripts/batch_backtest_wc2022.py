#!/usr/bin/env python
"""
Batch backtest Mundial 2022 — alimenta /patterns con 10 partidos verificados.
Corre contra el servidor local (localhost:5000).

Uso:
    python scripts/batch_backtest_wc2022.py            # corre todo
    python scripts/batch_backtest_wc2022.py --dry-run  # solo muestra las requests, no las corre
"""
from __future__ import annotations

import sys
import time

import requests

SERVER = "http://localhost:5000"
API_FOOTBALL_KEY = "ae67981f4644a3a057958c17c61ac654"
API_FOOTBALL_STATUS = "https://v3.football.api-sports.io/status"

# ---------------------------------------------------------------------------
# Lista de partidos verificados — Mundial Qatar 2022
# Ordenados para maximizar reutilización de cache:
#   · Argentina aparece en partidos 0 y 3; al correrlos consecutivos el 2º
#     aprovecha el caché de fixtures/stats del 1º.
#   · Misma lógica para equipos con mucho historial (France, Brazil, Germany).
# Campo: (home, away, date, final_score, actual_corners, actual_cards, nota)
# ---------------------------------------------------------------------------
MATCHES = [
    ("Argentina",    "Australia",     "2022-12-03", "2-1", None, None, "Favorito gana ajustado"),
    ("Argentina",    "Saudi Arabia",  "2022-11-22", "1-2", None, None, "SORPRESA - favorito pierde"),
    ("England",      "Senegal",       "2022-12-04", "3-0", None, None, "Favorito gana comodo"),
    ("Spain",        "Costa Rica",    "2022-11-23", "7-0", None, None, "Goleada extrema"),
    ("Japan",        "Germany",       "2022-11-23", "2-1", None, None, "SORPRESA - favorito pierde"),
    ("France",       "Denmark",       "2022-11-26", "2-1", None, None, "Favorito gana ajustado"),
    ("Brazil",       "Switzerland",   "2022-11-28", "1-0", None, None, "Favorito gana minimo"),
    ("Portugal",     "Uruguay",       "2022-11-28", "2-0", None, None, "Favorito gana"),
    ("Netherlands",  "Qatar",         "2022-11-29", "2-0", None, None, "Favorito gana comodo"),
    ("Morocco",      "Croatia",       "2022-11-23", "0-0", None, None, "Empate sin goles"),
]


def _get_rate_remaining() -> int:
    """Consulta cuántos requests quedan en la API de api-sports.io hoy."""
    try:
        r = requests.get(
            API_FOOTBALL_STATUS,
            headers={"x-apisports-key": API_FOOTBALL_KEY},
            timeout=10,
        )
        data = r.json().get("response", {}).get("requests", {})
        return int(data.get("limit_day", 100)) - int(data.get("current", 0))
    except Exception:
        return -1


def _run_backtest(home: str, away: str, date: str, score: str,
                  corners, cards) -> dict | None:
    payload = {
        "home_team": home,
        "away_team": away,
        "competition": "World Cup",
        "date": date,
        "neutral_venue": True,
        "known_result": {
            "final_score": score,
        },
    }
    if corners is not None:
        payload["known_result"]["actual_corners"] = corners
    if cards is not None:
        payload["known_result"]["actual_cards"] = cards

    try:
        r = requests.post(f"{SERVER}/backtest", json=payload, timeout=60)
        if r.status_code != 200:
            print(f"  ERROR HTTP {r.status_code}: {r.text[:200]}")
            return None
        return r.json()
    except requests.RequestException as exc:
        print(f"  ERROR request: {exc}")
        return None


def _format_accuracy(result: dict) -> str:
    acc = result.get("accuracy", {})
    rate = acc.get("accuracy_rate")
    hits = acc.get("hits", 0)
    total = acc.get("total_evaluable_legs", 0)
    combo = acc.get("combo_hits", "?/?")
    pct = f"{rate*100:.0f}%" if rate is not None else "n/a"
    return f"patas {hits}/{total} ({pct})  combo {combo}"


def main():
    dry_run = "--dry-run" in sys.argv

    print("=" * 70)
    print("BATCH BACKTEST - MUNDIAL 2022  (neutral_venue=True)")
    print("=" * 70)

    if dry_run:
        print("\n[DRY RUN] Solo mostrando partidos. No se ejecutan requests.\n")
        for i, (h, a, d, s, *_) in enumerate(MATCHES, 1):
            print(f"  {i:2d}. {h} vs {a}  ({d})  resultado={s}")
        print(f"\nTotal: {len(MATCHES)} partidos")
        return

    # Verificar servidor
    try:
        requests.get(f"{SERVER}/health", timeout=5)
    except Exception:
        print(f"\nERROR: El servidor no responde en {SERVER}")
        print("       Iniciá el servidor con: python run.py")
        sys.exit(1)

    rate_before = _get_rate_remaining()
    print(f"\nRate API-Football antes del batch: {rate_before}/100 disponibles")

    # Estimar consumo: ~5-7 req para equipos nuevos, ~1-2 para cacheados.
    # Argentina (matches 0+1) ahorra ~4 req por la segunda aparición.
    # England/Senegal ya estaban cacheados desde sesión anterior.
    print("Estimado: ~35-50 requests (con cache disk activo).")
    if 0 <= rate_before < 5:
        print(f"\nADVERTENCIA: solo quedan {rate_before} requests. El batch podría agotar el cupo.")
        print("Abortando. Esperá al reset diario o usá --dry-run.")
        return
    if rate_before < 0:
        print("Nota: rate limit no disponible (posible cuota agotada). Continuando desde cache.")

    print()
    results_summary = []

    for i, (home, away, date, score, corners, cards, notes) in enumerate(MATCHES, 1):
        rate_pre = _get_rate_remaining()
        print(f"[{i:2d}/10] {home} vs {away}  ({date})  ->  {score}  | {notes}")

        result = _run_backtest(home, away, date, score, corners, cards)

        rate_post = _get_rate_remaining()
        req_used = rate_pre - rate_post

        if result:
            dq = result.get("data_quality", {})
            sources = dq.get("sources_used", [])
            warnings = dq.get("warnings", [])
            acc_str = _format_accuracy(result)
            pid = result.get("prediction_id", "")[:8]
            print(f"       id={pid}  sources={sources}  req_usados={req_used}")
            print(f"       {acc_str}")
            if warnings:
                print(f"       warnings: {warnings}")
            results_summary.append({
                "match": f"{home} vs {away}",
                "score": score,
                "notes": notes,
                "sources": sources,
                "req_used": req_used,
                "accuracy": result.get("accuracy", {}),
                "combo_hits": result.get("accuracy", {}).get("combo_hits", "?/?"),
            })
        else:
            print("       FALLIDO")
            results_summary.append({"match": f"{home} vs {away}", "score": score, "failed": True, "req_used": req_used})

        print()
        # Pausa entre partidos. El free tier de api-sports.io tiene un límite
        # de ~10 req/min además del de 100/día. Cada partido nuevo hace 3-5 API calls
        # (team_id + fixtures + stats×2 + h2h). Con 8s de pausa entre partidos,
        # el flujo máximo es ~37 req/min (5 calls / 8s), dentro del margen.
        if i < len(MATCHES):
            time.sleep(8)

    # Resumen final
    rate_after = _get_rate_remaining()
    total_req_used = rate_before - rate_after

    print("=" * 70)
    print("RESUMEN DEL BATCH")
    print("=" * 70)
    print(f"Partidos procesados:  {len(results_summary)}/10")
    print(f"Requests API usados:  {total_req_used}  (quedan {rate_after}/100)")
    print()

    # Tabla de aciertos
    print(f"{'Partido':<35} {'Score':>6}  {'Patas':>10}  {'Combo':>8}")
    print("-" * 65)
    for r in results_summary:
        if r.get("failed"):
            print(f"  {r['match']:<33} {'FALLIDO':>6}")
            continue
        acc = r.get("accuracy", {})
        hits = acc.get("hits", 0)
        total = acc.get("total_evaluable_legs", 0)
        rate = acc.get("accuracy_rate")
        pct = f"{rate*100:.0f}%" if rate is not None else "n/a"
        print(f"  {r['match']:<33} {r['score']:>6}  {hits}/{total} ({pct})  {r['combo_hits']:>8}")

    print()

    # Obtener /patterns
    print("=" * 70)
    print("GET /patterns  (curva de calibracion)")
    print("=" * 70)
    try:
        pr = requests.get(f"{SERVER}/patterns", timeout=10)
        patterns = pr.json()
        print(f"\nPredicciones con resultado registrado: {patterns.get('total_predictions_with_results', 0)}")
        print()
        for mkt in patterns.get("markets", []):
            acc_val = mkt.get("accuracy")
            acc_pct = f"{acc_val*100:.1f}%" if acc_val is not None else "n/a"
            print(f"  {mkt['market']:<25}  total={mkt['total_predictions']:>3}  acierto={acc_pct}")
            for cal in mkt.get("calibration", []):
                pred = cal["predicted_prob"]
                actual = cal["actual_rate"]
                n = cal["total_samples"]
                err = cal.get("calibration_error")
                if actual is not None:
                    bar = "#" * round(actual * 10)
                    actual_pct = f"{actual*100:.0f}%"
                    err_str = f"err={err:.2f}" if err is not None else ""
                    print(f"    pred={pred:.1f}  real={actual_pct:>4}  n={n:>2}  {err_str:<10} {bar}")
            print()
        print(f"Nota: {patterns.get('note', '')}")
    except Exception as exc:
        print(f"ERROR al obtener /patterns: {exc}")


if __name__ == "__main__":
    main()
