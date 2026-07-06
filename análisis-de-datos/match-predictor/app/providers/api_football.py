"""
Proveedor principal: API-Football (api-sports.io).
Free tier: ~100 req/día sin tarjeta.
Key en header x-apisports-key.

Todos los requests pasan por core/cache.py con TTL apropiado.
Los límites de rate restantes se logean para que el usuario sepa cuántos quedan.
"""
from __future__ import annotations

import collections
import time
from typing import Optional

import requests

from app.core import cache
from app.core.config import API_FOOTBALL_KEY, CACHE_TTL
from app.core.logger import get_logger
from app.models.poisson import estimate_lambdas_from_stats
from app.providers.base import MatchData, StatsProvider

log = get_logger(__name__)

BASE_URL = "https://v3.football.api-sports.io"

# ---------------------------------------------------------------------------
# Rate limiter para el free tier de api-sports.io (10 req/min, 100 req/día).
# Implementación token-bucket simplificada con ventana deslizante de 60s.
# ---------------------------------------------------------------------------
_RATE_LIMIT_PER_MIN = 10        # requests por ventana
_RATE_WINDOW_SECONDS = 62       # ventana ligeramente mayor a 60s (margen de seguridad)
_api_call_times: collections.deque = collections.deque(maxlen=_RATE_LIMIT_PER_MIN)


def _rate_limit_wait() -> None:
    """
    Bloquea hasta que haya cupo para una nueva llamada.
    Si el deque ya tiene _RATE_LIMIT_PER_MIN entradas y la más antigua
    tiene menos de _RATE_WINDOW_SECONDS de antigüedad, duerme el tiempo restante.
    """
    now = time.monotonic()
    if len(_api_call_times) == _RATE_LIMIT_PER_MIN:
        oldest = _api_call_times[0]
        wait = _RATE_WINDOW_SECONDS - (now - oldest)
        if wait > 0:
            log.info("[RateLimit] Ventana de %d req/min alcanzada. Esperando %.1fs...", _RATE_LIMIT_PER_MIN, wait)
            time.sleep(wait)
    _api_call_times.append(time.monotonic())

# Mapeo de nombres de competición → league_id en API-Football.
# Solo los más comunes; se puede ampliar sin tocar la lógica.
_COMPETITION_LEAGUE_MAP: dict[str, int] = {
    # Internacionales
    "world cup": 1,
    "mundial": 1,
    "copa del mundo": 1,
    "uefa champions league": 2,
    "champions league": 2,
    "ucl": 2,
    "uefa europa league": 3,
    "europa league": 3,
    "uel": 3,
    "uefa conference league": 848,
    "conference league": 848,
    # Ligas domésticas principales
    "premier league": 39,
    "la liga": 140,
    "bundesliga": 78,
    "serie a": 135,
    "ligue 1": 61,
    "eredivisie": 88,
    "primeira liga": 94,
    "mls": 253,
    "liga mx": 262,
    "liga bbva mx": 262,
}


class ApiFootballProvider(StatsProvider):
    """Adaptador para api-sports.io v3."""

    @property
    def name(self) -> str:
        return "api_football"

    def _headers(self) -> dict:
        return {"x-apisports-key": API_FOOTBALL_KEY}

    def _get(self, endpoint: str, params: dict, cache_key: str, ttl: int) -> Optional[dict]:
        """GET con cache. Loguea requests restantes del rate limit."""
        cached = cache.get(cache_key)
        if cached is not None:
            return cached

        if not API_FOOTBALL_KEY:
            log.warning("API_FOOTBALL_KEY no configurada. Usando datos mock.")
            return None

        url = f"{BASE_URL}/{endpoint}"
        try:
            _rate_limit_wait()  # Respetar el límite de 10 req/min del free tier
            resp = requests.get(url, headers=self._headers(), params=params, timeout=10)
            remaining = resp.headers.get("x-ratelimit-requests-remaining", "?")
            log.info("[API-Football] GET %s -> %d | Rate restante: %s", endpoint, resp.status_code, remaining)

            if resp.status_code != 200:
                log.error("[API-Football] Error %d: %s", resp.status_code, resp.text[:200])
                return None

            data = resp.json()
            if data.get("errors"):
                log.error("[API-Football] Errores en respuesta: %s", data["errors"])
                return None

            # No cachear respuestas vacías: podrían ser errores transitorios (throttle,
            # plan restriction) que se resolverían en el siguiente intento.
            if not data.get("response") and data.get("results", 0) == 0:
                log.debug("[API-Football] Respuesta vacía para %s — no se cachea.", endpoint)
                return data  # Sí la retornamos (puede ser válida), pero sin cachear

            cache.set(cache_key, data, ttl)
            return data
        except requests.RequestException as exc:
            log.error("[API-Football] Request fallido: %s", exc)
            return None

    def _resolve_team_id(self, team_name: str) -> Optional[int]:
        """Resuelve nombre de equipo → ID via /teams?search=. Caché de fixtures TTL."""
        cache_key = f"apif_teamid_{team_name.lower().replace(' ', '_')}"
        data = self._get("teams", {"search": team_name}, cache_key, CACHE_TTL["fixtures"])
        if not data:
            return None
        results = data.get("response", [])
        if not results:
            log.warning("[API-Football] No se encontró equipo: %s", team_name)
            return None
        # El primer resultado de la búsqueda de equipos nacionales suele ser el correcto.
        # Preferir el que coincida exactamente por nombre (case-insensitive).
        name_lower = team_name.lower()
        for item in results:
            if item.get("team", {}).get("name", "").lower() == name_lower:
                return item["team"]["id"]
        return results[0]["team"]["id"]

    @staticmethod
    def _derive_season(date: str) -> int:
        """
        Deriva la season (año) desde la fecha del partido.
        Para ligas que arrancan en verano (ej. Premier League 2022-23),
        si el mes es >= 7 se usa el año actual, sino el año anterior.
        Para torneos con año fijo (World Cup, Copa América) el año del fixture es el season.
        La lógica conservadora: devolver siempre int(date[:4]).
        API-Football usa el año de inicio del torneo como season en todos los casos.
        """
        return int(date[:4])

    def search_fixture(self, home_team: str, away_team: str, date: str,
                       competition: str = "") -> Optional[dict]:
        """
        Busca el fixture por IDs de equipo + league + season, filtrando por fecha.

        Estrategia (en orden):
        1. Resolver league_id desde competition (si se conoce).
        2. Resolver home_team → team_id.
        3. GET /fixtures?team={id}&league={lid}&season={year}  (más preciso)
           o GET /fixtures?team={id}&season={year}             (fallback sin league)
        4. Filtrar los resultados por fecha y nombre del away team.

        Esto evita usar el parámetro `search` que no existe en /fixtures,
        y evita usar `date` como param principal (el free tier solo permite rango reciente).
        """
        cache_key = f"apif_fixture_{home_team}_{away_team}_{date}".replace(" ", "_")
        cached = cache.get(cache_key)
        if cached is not None:
            return cached

        # 1. Mapear competition a league_id
        league_id: Optional[int] = _COMPETITION_LEAGUE_MAP.get(competition.lower().strip())

        # 2. Resolver home team ID
        home_id = self._resolve_team_id(home_team)
        if not home_id:
            log.warning("[API-Football] No se pudo resolver ID de %s", home_team)
            return None

        # 3. Derivar season
        season = self._derive_season(date)

        # 4. Buscar fixtures del equipo local en esa liga/temporada
        params: dict = {"team": home_id, "season": season}
        if league_id:
            params["league"] = league_id
            ep_cache_key = f"apif_fixtures_team{home_id}_l{league_id}_s{season}"
        else:
            ep_cache_key = f"apif_fixtures_team{home_id}_s{season}"

        log.info(
            "[API-Football] search_fixture: team=%s(%s) league=%s season=%s date=%s away=%s",
            home_team, home_id, league_id, season, date, away_team,
        )

        data = self._get("fixtures", params, ep_cache_key, CACHE_TTL["fixtures"])
        if not data:
            return None

        fixtures = data.get("response", [])
        away_lower = away_team.lower()

        # 5. Filtrar por fecha y nombre del otro equipo.
        # El endpoint /fixtures?team=X devuelve partidos donde X aparece como local O visitante,
        # por lo que hay que chequear ambos lados para encontrar al otro equipo.
        for fixture in fixtures:
            fix_date = fixture.get("fixture", {}).get("date", "")[:10]
            teams = fixture.get("teams", {})
            fix_home = teams.get("home", {}).get("name", "").lower()
            fix_away = teams.get("away", {}).get("name", "").lower()
            other_matches = (away_lower in fix_away or fix_away in away_lower or
                             away_lower in fix_home or fix_home in away_lower)
            if fix_date == date and other_matches:
                cache.set(cache_key, fixture, CACHE_TTL["fixtures"])
                log.info(
                    "[API-Football] Fixture encontrado: id=%s  %s vs %s",
                    fixture["fixture"]["id"], teams["home"]["name"], teams["away"]["name"],
                )
                return fixture

        # 6. Fallback via el equipo visitante si step 5 no encontró nada
        # (también cubre casos sin league_id o sin fixtures del equipo local)
        if True:
            away_id = self._resolve_team_id(away_team)
            if away_id:
                params2: dict = {"team": away_id, "season": season}
                ep_cache_key2 = f"apif_fixtures_team{away_id}_s{season}"
                data2 = self._get("fixtures", params2, ep_cache_key2, CACHE_TTL["fixtures"])
                for fixture in (data2 or {}).get("response", []):
                    fix_date = fixture.get("fixture", {}).get("date", "")[:10]
                    teams = fixture.get("teams", {})
                    home_name = teams.get("home", {}).get("name", "").lower()
                    if fix_date == date and (home_team.lower() in home_name or home_name in home_team.lower()):
                        cache.set(cache_key, fixture, CACHE_TTL["fixtures"])
                        log.info(
                            "[API-Football] Fixture encontrado (via away): id=%s  %s vs %s",
                            fixture["fixture"]["id"], teams["home"]["name"], teams["away"]["name"],
                        )
                        return fixture

        log.warning("[API-Football] No se encontró fixture %s vs %s en %s", home_team, away_team, date)
        return None

    def get_team_stats(self, team_id: int, league_id: int, season: int) -> Optional[dict]:
        cache_key = f"apif_teamstats_{team_id}_{league_id}_{season}"
        return self._get(
            "teams/statistics",
            {"team": team_id, "league": league_id, "season": season},
            cache_key,
            CACHE_TTL["statistics"],
        )

    def get_h2h(self, team1_id: int, team2_id: int) -> Optional[dict]:
        cache_key = f"apif_h2h_{team1_id}_{team2_id}"
        # Free tier no soporta `last`; usar solo h2h sin ese parámetro
        return self._get(
            "fixtures/headtohead",
            {"h2h": f"{team1_id}-{team2_id}"},
            cache_key,
            CACHE_TTL["h2h"],
        )

    def get_odds(self, fixture_id: int) -> Optional[dict]:
        cache_key = f"apif_odds_{fixture_id}"
        return self._get(
            "odds",
            {"fixture": fixture_id},
            cache_key,
            CACHE_TTL["odds"],
        )

    def get_team_info(self, team_id: int) -> Optional[dict]:
        """Info del equipo (incluye su estadio habitual). Caché 7 días."""
        cache_key = f"apif_teaminfo_{team_id}"
        return self._get(
            "teams",
            {"id": team_id},
            cache_key,
            CACHE_TTL["team_info"],
        )

    def get_match_data(self, home_team: str, away_team: str,
                       competition: str, date: str) -> Optional[MatchData]:
        md = MatchData(
            home_team=home_team,
            away_team=away_team,
            competition=competition,
            date=date,
            sources_used=[],
        )

        fixture = self.search_fixture(home_team, away_team, date, competition=competition)
        if not fixture:
            md.warnings.append(f"Fixture no encontrado en API-Football para {home_team} vs {away_team} el {date}")
            # Retornar con lambdas por defecto para que el motor igualmente funcione
            md.lambda_home, md.lambda_away = estimate_lambdas_from_stats(
                md.home_goals_scored_avg, md.home_goals_conceded_avg,
                md.away_goals_scored_avg, md.away_goals_conceded_avg,
            )
            return md

        md.fixture_id = fixture.get("fixture", {}).get("id")
        md.sources_used.append(self.name)

        # Extraer IDs de liga y temporada
        league = fixture.get("league", {})
        league_id = league.get("id")
        season = league.get("season")
        teams = fixture.get("teams", {})
        home_id = teams.get("home", {}).get("id")
        away_id = teams.get("away", {}).get("id")

        # Detectar campo neutral: venue del fixture ≠ estadio habitual del equipo local
        fixture_venue_id = fixture.get("fixture", {}).get("venue", {}).get("id")
        if fixture_venue_id and home_id:
            home_info = self.get_team_info(home_id)
            if home_info:
                home_venue_id = (home_info.get("response") or [{}])[0].get("venue", {}).get("id")
                if home_venue_id and fixture_venue_id != home_venue_id:
                    md.is_neutral_venue = True
                    log.info(
                        "[API-Football] Campo neutral detectado para %s vs %s: "
                        "fixture_venue=%s ≠ home_venue=%s",
                        home_team, away_team, fixture_venue_id, home_venue_id,
                    )

        # Stats de temporada para cada equipo
        if home_id and league_id and season:
            home_stats = self.get_team_stats(home_id, league_id, season)
            away_stats = self.get_team_stats(away_id, league_id, season)
            self._fill_team_stats(md, home_stats, away_stats)
        else:
            md.missing.append("team_season_stats")

        # H2H
        if home_id and away_id:
            h2h_data = self.get_h2h(home_id, away_id)
            self._fill_h2h(md, h2h_data)
        else:
            md.missing.append("h2h")

        # Cuotas
        if md.fixture_id:
            odds_data = self.get_odds(md.fixture_id)
            self._fill_odds(md, odds_data)
        else:
            md.warnings.append("cuotas no disponibles: fixture_id desconocido")

        # Calcular lambdas finales (sin ventaja local en campo neutral)
        md.lambda_home, md.lambda_away = estimate_lambdas_from_stats(
            md.home_goals_scored_avg,
            md.home_goals_conceded_avg,
            md.away_goals_scored_avg,
            md.away_goals_conceded_avg,
            md.league_avg_goals,
            home_advantage=1.0 if md.is_neutral_venue else 1.2,
        )
        return md

    # Regularización Bayesiana para muestras pequeñas (torneos, copas).
    # Con n partidos, el promedio se encoge hacia BASE_GOALS con peso K.
    # Fórmula: shrunk = (n * avg + K * base) / (n + K)
    # K=5: con 5 partidos el equipo pesa igual que la media base; con 38, casi no cambia.
    _SHRINK_K: float = 5.0
    _SHRINK_BASE_SCORED: float = 1.35    # media base goles marcados (selecciones nacionales)
    _SHRINK_BASE_CONCEDED: float = 1.35  # media base goles concedidos

    def _fill_team_stats(self, md: MatchData, home_raw: Optional[dict], away_raw: Optional[dict]):
        def _extract_goals(raw: Optional[dict]) -> tuple[float, float, int]:
            """
            Retorna (scored_avg, conceded_avg, n_matches) desde el JSON de team statistics.
            n_matches se usa para regularización; default 20 si no está disponible.
            """
            if not raw:
                return 1.3, 1.1, 20
            resp = raw.get("response", {})
            stats = resp.get("goals", {})
            scored = stats.get("for", {}).get("average", {}).get("total")
            conceded = stats.get("against", {}).get("average", {}).get("total")
            n = resp.get("fixtures", {}).get("played", {}).get("total")
            try:
                n_int = max(int(n or 20), 1)
                return float(scored or 1.3), float(conceded or 1.1), n_int
            except (TypeError, ValueError):
                return 1.3, 1.1, 20

        def _shrink(avg: float, n: int, base: float) -> float:
            """Shrinkage hacia la media base: (n·avg + K·base) / (n + K)."""
            return (n * avg + self._SHRINK_K * base) / (n + self._SHRINK_K)

        def _extract_corners(raw: Optional[dict]) -> float:
            if not raw:
                return 5.5
            # API-Football no siempre tiene corners en team stats; usar default si no hay
            return 5.5

        def _extract_cards(raw: Optional[dict]) -> float:
            if not raw:
                return 2.0
            stats = raw.get("response", {}).get("cards", {})
            yellow = stats.get("yellow", {})
            # Sumar yellows como proxy de tarjetas por partido
            total = sum(v.get("total") or 0 for v in yellow.values() if isinstance(v, dict))
            games = raw.get("response", {}).get("fixtures", {}).get("played", {}).get("total", 1)
            try:
                avg = total / max(int(games), 1)
                return max(1.0, min(avg, 5.0))
            except Exception:
                return 2.0

        home_scored_raw, home_conceded_raw, n_home = _extract_goals(home_raw)
        away_scored_raw, away_conceded_raw, n_away = _extract_goals(away_raw)

        # Aplicar shrinkage — encoge hacia la media base en proporción inversa a n
        md.home_goals_scored_avg   = _shrink(home_scored_raw,   n_home, self._SHRINK_BASE_SCORED)
        md.home_goals_conceded_avg = _shrink(home_conceded_raw, n_home, self._SHRINK_BASE_CONCEDED)
        md.away_goals_scored_avg   = _shrink(away_scored_raw,   n_away, self._SHRINK_BASE_SCORED)
        md.away_goals_conceded_avg = _shrink(away_conceded_raw, n_away, self._SHRINK_BASE_CONCEDED)

        log.info(
            "[Stats] home: scored %.2f->%.2f conceded %.2f->%.2f (n=%d)  "
            "away: scored %.2f->%.2f conceded %.2f->%.2f (n=%d)",
            home_scored_raw, md.home_goals_scored_avg,
            home_conceded_raw, md.home_goals_conceded_avg, n_home,
            away_scored_raw, md.away_goals_scored_avg,
            away_conceded_raw, md.away_goals_conceded_avg, n_away,
        )

        md.lambda_corners_home = _extract_corners(home_raw)
        md.lambda_corners_away = _extract_corners(away_raw)
        md.lambda_cards_home = _extract_cards(home_raw)
        md.lambda_cards_away = _extract_cards(away_raw)

        # Fuerza relativa: diferencia normalizada de goles esperados (post-shrinkage)
        diff = md.home_goals_scored_avg - md.away_goals_scored_avg
        md.fuerza_relativa = max(-1.0, min(diff / 2.0, 1.0))

        md.sources_used.append("api_football_team_stats")

    def _fill_h2h(self, md: MatchData, h2h_raw: Optional[dict]):
        if not h2h_raw:
            md.missing.append("h2h_data")
            return
        fixtures = h2h_raw.get("response", [])
        if not fixtures:
            md.missing.append("h2h_data")
            return

        home_wins = away_wins = draws = 0
        for f in fixtures[-10:]:  # últimos 10 encuentros
            score = f.get("score", {}).get("fulltime", {})
            h_goals = score.get("home") or 0
            a_goals = score.get("away") or 0
            if h_goals > a_goals:
                home_wins += 1
            elif a_goals > h_goals:
                away_wins += 1
            else:
                draws += 1

        total = max(home_wins + away_wins + draws, 1)
        # H2H advantage: positivo si local ha ganado más, negativo si visitante
        md.h2h_advantage = round((home_wins - away_wins) / total, 2)
        md.sources_used.append("api_football_h2h")

    def _fill_odds(self, md: MatchData, odds_raw: Optional[dict]):
        if not odds_raw:
            md.warnings.append("cuotas no disponibles para este partido")
            return
        bookmakers = odds_raw.get("response", [])
        if not bookmakers:
            md.warnings.append("cuotas no disponibles para este partido")
            return

        # Tomar el primer bookmaker disponible
        bm = bookmakers[0].get("bookmakers", [{}])[0] if bookmakers else {}
        bets = bm.get("bets", [])

        for bet in bets:
            if bet.get("name") == "Match Winner":
                for value in bet.get("values", []):
                    outcome = value.get("value", "")
                    odd = float(value.get("odd", 0) or 0)
                    if outcome == "Home":
                        md.odds["home_decimal"] = odd
                    elif outcome == "Away":
                        md.odds["away_decimal"] = odd
                    elif outcome == "Draw":
                        md.odds["draw_decimal"] = odd

        md.sources_used.append("api_football_odds")

    def health_check(self) -> dict:
        if not API_FOOTBALL_KEY:
            return {"ok": False, "provider": self.name, "message": "API key no configurada"}
        data = self._get("status", {}, "apif_status", 60)
        if data:
            account = data.get("response", {}).get("account", {})
            requests_info = data.get("response", {}).get("requests", {})
            return {
                "ok": True,
                "provider": self.name,
                "account": account.get("firstname", ""),
                "requests_used": requests_info.get("current", 0),
                "requests_limit": requests_info.get("limit_day", 100),
            }
        return {"ok": False, "provider": self.name, "message": "No se pudo conectar"}
