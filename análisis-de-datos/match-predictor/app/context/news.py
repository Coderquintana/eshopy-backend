"""
Enriquecedor de contexto cualitativo.

Convierte texto libre (manual_context, noticias) en señales estructuradas
que ajustan los pesos del modelo.

Ejemplo:
  "el portero titular está lesionado y el equipo viene de tres derrotas"
  → {injury: keeper, morale: low}

La IA (Groq/Gemini/local) se aísla detrás de ContextEnricher.
Si no hay key configurada, se usa sólo el manual_context con parsing básico.
El sistema SIEMPRE funciona sin IA (degradación elegante).

La IA NO calcula probabilidades. Solo estructura contexto.
Las probabilidades siempre salen de los modelos estadísticos (poisson.py).
"""
from __future__ import annotations

from typing import Optional

from app.core.config import GROQ_API_KEY, GEMINI_API_KEY
from app.core.logger import get_logger

log = get_logger(__name__)


class ContextSignals:
    """Señales estructuradas extraídas del contexto cualitativo."""
    injury_keeper_home: bool = False
    injury_keeper_away: bool = False
    injury_striker_home: bool = False
    injury_striker_away: bool = False
    morale_home: str = "neutral"    # "high" | "neutral" | "low"
    morale_away: str = "neutral"
    rotation_home: bool = False     # El equipo local rota jugadores
    rotation_away: bool = False
    key_player_out_home: bool = False
    key_player_out_away: bool = False
    raw: dict = None

    def __init__(self):
        self.raw = {}

    def to_weight_adjustments(self) -> dict:
        """
        Convierte señales en ajustes multiplicativos para los lambdas del modelo.
        Retorna dict con factores de ajuste:
          lambda_home_factor, lambda_away_factor,
          cards_home_factor, cards_away_factor.
        """
        lh_factor = 1.0
        la_factor = 1.0
        cards_h = 1.0
        cards_a = 1.0

        # Portero titular lesionado → equipo concede más goles
        if self.injury_keeper_home:
            la_factor *= 1.15   # Visitante anota más
            log.info("Ajuste: portero local lesionado → la_factor +15%%")
        if self.injury_keeper_away:
            lh_factor *= 1.15

        # Delantero estrella lesionado → equipo marca menos
        if self.injury_striker_home:
            lh_factor *= 0.85
            log.info("Ajuste: delantero local fuera → lh_factor -15%%")
        if self.injury_striker_away:
            la_factor *= 0.85

        # Rotación → rendimiento ligeramente inferior
        if self.rotation_home:
            lh_factor *= 0.90
        if self.rotation_away:
            la_factor *= 0.90

        # Estado anímico
        morale_map = {"high": 1.10, "neutral": 1.0, "low": 0.90}
        lh_factor *= morale_map.get(self.morale_home, 1.0)
        la_factor *= morale_map.get(self.morale_away, 1.0)

        return {
            "lambda_home_factor": round(lh_factor, 3),
            "lambda_away_factor": round(la_factor, 3),
            "cards_home_factor": round(cards_h, 3),
            "cards_away_factor": round(cards_a, 3),
        }


class ContextEnricher:
    """
    Interfaz para enriquecer contexto. Usa IA si está disponible, sino parsing básico.
    """

    def enrich(self, manual_context: Optional[dict]) -> ContextSignals:
        signals = ContextSignals()
        if not manual_context:
            return signals

        injuries = manual_context.get("injuries", [])
        notes = manual_context.get("notes", "")

        # Parsing básico sin IA
        signals = self._parse_basic(injuries, notes)

        # Si hay IA disponible, refinar
        if GROQ_API_KEY:
            signals = self._enrich_with_groq(manual_context, signals)
        elif GEMINI_API_KEY:
            signals = self._enrich_with_gemini(manual_context, signals)

        return signals

    def _parse_basic(self, injuries: list[str], notes: str) -> ContextSignals:
        """Parsing por palabras clave. No depende de IA."""
        signals = ContextSignals()
        all_text = " ".join(injuries) + " " + notes
        text_lower = all_text.lower()

        signals.injury_keeper_home = (
            "portero" in text_lower or "keeper" in text_lower or "goalkeeper" in text_lower
        ) and ("home" in text_lower or "local" in text_lower)

        signals.injury_keeper_away = (
            "portero" in text_lower or "keeper" in text_lower or "goalkeeper" in text_lower
        ) and ("away" in text_lower or "visitante" in text_lower)

        signals.rotation_home = (
            "rota" in text_lower or "rotate" in text_lower or "rotation" in text_lower
        ) and ("home" in text_lower or "local" in text_lower)

        signals.rotation_away = (
            "rota" in text_lower or "rotate" in text_lower or "rotation" in text_lower
        ) and ("away" in text_lower or "visitante" in text_lower)

        if "tres derrotas" in text_lower or "bad form" in text_lower or "mala racha" in text_lower:
            # Determinar si es home o away por contexto
            if "home" in text_lower or "local" in text_lower:
                signals.morale_home = "low"
            else:
                signals.morale_away = "low"

        signals.raw = {"injuries": injuries, "notes": notes}
        return signals

    def _enrich_with_groq(self, manual_context: dict, signals: ContextSignals) -> ContextSignals:
        """Refinamiento con Groq (free tier). Extrae señales con LLM."""
        try:
            import requests
            prompt = (
                "Analiza este contexto de partido de fútbol y responde SOLO en JSON con estas claves: "
                "injury_keeper_home (bool), injury_keeper_away (bool), injury_striker_home (bool), "
                "injury_striker_away (bool), morale_home (high/neutral/low), morale_away (high/neutral/low), "
                "rotation_home (bool), rotation_away (bool).\n\n"
                f"Contexto: {manual_context}"
            )
            resp = requests.post(
                "https://api.groq.com/openai/v1/chat/completions",
                headers={"Authorization": f"Bearer {GROQ_API_KEY}", "Content-Type": "application/json"},
                json={
                    "model": "llama-3.1-8b-instant",
                    "messages": [{"role": "user", "content": prompt}],
                    "temperature": 0,
                },
                timeout=10,
            )
            if resp.status_code == 200:
                import json
                content = resp.json()["choices"][0]["message"]["content"]
                # Extraer JSON de la respuesta
                start = content.find("{")
                end = content.rfind("}") + 1
                if start >= 0 and end > start:
                    parsed = json.loads(content[start:end])
                    signals.injury_keeper_home = bool(parsed.get("injury_keeper_home", signals.injury_keeper_home))
                    signals.injury_keeper_away = bool(parsed.get("injury_keeper_away", signals.injury_keeper_away))
                    signals.morale_home = parsed.get("morale_home", signals.morale_home)
                    signals.morale_away = parsed.get("morale_away", signals.morale_away)
                    signals.rotation_home = bool(parsed.get("rotation_home", signals.rotation_home))
                    signals.rotation_away = bool(parsed.get("rotation_away", signals.rotation_away))
                    log.info("Contexto enriquecido con Groq.")
        except Exception as exc:
            log.warning("Groq enrichment fallido, usando parsing básico: %s", exc)
        return signals

    def _enrich_with_gemini(self, manual_context: dict, signals: ContextSignals) -> ContextSignals:
        """Placeholder para Gemini. Misma interfaz que Groq."""
        log.debug("Gemini enrichment no implementado aún.")
        return signals
