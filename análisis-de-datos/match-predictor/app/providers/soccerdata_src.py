"""
Proveedor de xG y stats profundos vía librería `soccerdata` (FBref/Sofascore).
No requiere API key: scrapea las fuentes públicas.

IMPORTANTE: respetar rate limits agresivamente.
NO paralelizar requests. Cachear días, no minutos.
Uso personal/educativo únicamente.

Este proveedor es OPCIONAL: si soccerdata no está instalado, el sistema funciona
sin él (degradación elegante).
"""
from __future__ import annotations

from typing import Optional

from app.core import cache
from app.core.config import CACHE_TTL
from app.core.logger import get_logger
from app.providers.base import MatchData, StatsProvider

log = get_logger(__name__)

try:
    import soccerdata as sd
    SOCCERDATA_AVAILABLE = True
except ImportError:
    SOCCERDATA_AVAILABLE = False
    log.info("soccerdata no instalado. El proveedor de xG estará desactivado.")


class SoccerDataProvider(StatsProvider):
    """Enriquece MatchData con xG de FBref."""

    @property
    def name(self) -> str:
        return "soccerdata_fbref"

    def get_match_data(self, home_team: str, away_team: str,
                       competition: str, date: str) -> Optional[MatchData]:
        if not SOCCERDATA_AVAILABLE:
            return None
        # Este proveedor es enricher, no proveedor principal
        return None

    def get_xg(self, team: str, season: str = "2425") -> Optional[dict]:
        """Intenta obtener xG del equipo desde FBref. Cachea 3 días."""
        if not SOCCERDATA_AVAILABLE:
            return None
        cache_key = f"fbref_xg_{team}_{season}".replace(" ", "_")
        cached = cache.get(cache_key)
        if cached:
            return cached
        try:
            log.info("[soccerdata] Obteniendo xG de FBref para %s (puede tardar)...", team)
            fbref = sd.FBref(leagues="ENG-Premier League", seasons=season)
            schedule = fbref.read_schedule()
            team_matches = schedule[
                (schedule["home_team"] == team) | (schedule["away_team"] == team)
            ]
            if team_matches.empty:
                return None
            result = {
                "xg_for": float(team_matches.get("xg_home", team_matches.get("xg", [1.3])).mean()),
                "xg_against": float(team_matches.get("xg_away", [1.0]).mean()),
            }
            cache.set(cache_key, result, CACHE_TTL["h2h"])
            return result
        except Exception as exc:
            log.warning("[soccerdata] Error obteniendo xG: %s", exc)
            return None

    def health_check(self) -> dict:
        return {
            "ok": SOCCERDATA_AVAILABLE,
            "provider": self.name,
            "message": "disponible" if SOCCERDATA_AVAILABLE else "soccerdata no instalado (opcional)",
        }
