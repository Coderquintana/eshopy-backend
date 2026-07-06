"""
Proveedor secundario: football-data.org
Free tier para top competitions (Premier League, La Liga, Bundesliga, etc.).
Úsalo como fallback o para validación cruzada.
"""
from __future__ import annotations

from typing import Optional

import requests

from app.core import cache
from app.core.config import FOOTBALL_DATA_KEY, CACHE_TTL
from app.core.logger import get_logger
from app.providers.base import MatchData, StatsProvider

log = get_logger(__name__)
BASE_URL = "https://api.football-data.org/v4"


class FootballDataProvider(StatsProvider):

    @property
    def name(self) -> str:
        return "football_data_org"

    def _headers(self) -> dict:
        return {"X-Auth-Token": FOOTBALL_DATA_KEY}

    def _get(self, endpoint: str, params: dict, cache_key: str, ttl: int) -> Optional[dict]:
        cached = cache.get(cache_key)
        if cached is not None:
            return cached

        if not FOOTBALL_DATA_KEY:
            log.debug("FOOTBALL_DATA_KEY no configurada, proveedor desactivado.")
            return None

        url = f"{BASE_URL}/{endpoint}"
        try:
            resp = requests.get(url, headers=self._headers(), params=params, timeout=10)
            log.info("[football-data] GET %s → %d", endpoint, resp.status_code)
            if resp.status_code == 429:
                log.warning("[football-data] Rate limit alcanzado.")
                return None
            if resp.status_code != 200:
                log.error("[football-data] Error %d", resp.status_code)
                return None
            data = resp.json()
            cache.set(cache_key, data, ttl)
            return data
        except requests.RequestException as exc:
            log.error("[football-data] Request fallido: %s", exc)
            return None

    def get_match_data(self, home_team: str, away_team: str,
                       competition: str, date: str) -> Optional[MatchData]:
        """
        football-data.org no permite búsqueda libre por nombre.
        Este proveedor es útil como enricher de standings/form; para predicción
        principal, usar api_football.
        """
        log.debug("[football-data] get_match_data llamado para %s vs %s", home_team, away_team)
        return None  # Fallback: retorna None para que se use api_football

    def health_check(self) -> dict:
        if not FOOTBALL_DATA_KEY:
            return {"ok": False, "provider": self.name, "message": "API key no configurada"}
        data = self._get("competitions", {}, "fd_competitions", 86400)
        if data:
            return {"ok": True, "provider": self.name, "competitions": len(data.get("competitions", []))}
        return {"ok": False, "provider": self.name, "message": "No se pudo conectar"}
