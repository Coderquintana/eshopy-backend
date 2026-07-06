"""
Interfaz abstracta StatsProvider.
Todo proveedor de datos implementa esta interfaz.
Así el motor de probabilidad no sabe de dónde vienen los datos.
"""
from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Optional


@dataclass
class MatchData:
    """
    Objeto normalizado que el motor recibe. Un único contrato entre proveedores y modelo.
    Si un campo no está disponible queda en None y se anota en missing[].
    """
    # Identidad del partido
    home_team: str = ""
    away_team: str = ""
    competition: str = ""
    date: str = ""
    fixture_id: Optional[int] = None

    # Lambdas calculadas (goles esperados)
    lambda_home: float = 1.3
    lambda_away: float = 1.0

    # Stats de goles (medias de la temporada)
    home_goals_scored_avg: float = 1.3
    home_goals_conceded_avg: float = 1.1
    away_goals_scored_avg: float = 1.0
    away_goals_conceded_avg: float = 1.3
    league_avg_goals: float = 1.3

    # xG
    xg_home: Optional[float] = None
    xg_away: Optional[float] = None

    # Córners
    lambda_corners_home: float = 5.5
    lambda_corners_away: float = 4.5
    avg_corners: Optional[float] = None

    # Tarjetas
    lambda_cards_home: float = 2.0
    lambda_cards_away: float = 1.8
    referee_cards_avg: Optional[float] = None

    # Factores cualitativos (-1..+1 desde perspectiva local)
    fuerza_relativa: float = 0.0
    forma_5: float = 0.0
    bajas_clave: float = 0.0
    h2h_advantage: float = 0.0
    localía: float = 0.1  # por defecto leve ventaja local
    importancia: float = 0.0

    # Jugadores para mercados de goleador
    players: list = field(default_factory=list)

    # Cuotas de casas de apuesta
    odds: dict = field(default_factory=dict)

    # Líneas configurables
    goals_line: float = 2.5
    corners_line: float = 9.5
    cards_line: float = 3.5

    # Cancha neutral (p.ej. Copa del Mundo, Eurocopa en sede neutral)
    is_neutral_venue: bool = False

    # Calidad del dato
    sources_used: list = field(default_factory=list)
    missing: list = field(default_factory=list)
    warnings: list = field(default_factory=list)

    def to_model_dict(self) -> dict:
        """Convierte a diccionario plano para pasarle a markets.py."""
        return {
            "home_team": self.home_team,
            "away_team": self.away_team,
            "lambda_home": self.lambda_home,
            "lambda_away": self.lambda_away,
            "lambda_corners_home": self.lambda_corners_home,
            "lambda_corners_away": self.lambda_corners_away,
            "lambda_cards_home": self.lambda_cards_home,
            "lambda_cards_away": self.lambda_cards_away,
            "xg_home": self.xg_home or self.lambda_home,
            "xg_away": self.xg_away or self.lambda_away,
            "avg_corners": self.avg_corners,
            "referee_cards_avg": self.referee_cards_avg,
            "fuerza_relativa": self.fuerza_relativa,
            "forma_5": self.forma_5,
            "bajas_clave": self.bajas_clave,
            "h2h_advantage": self.h2h_advantage,
            # En cancha neutral la ventaja de local = 0
            "localía": 0.0 if self.is_neutral_venue else self.localía,
            "importancia": self.importancia,
            "neutral_venue": self.is_neutral_venue,
            "players": self.players,
            "goals_line": self.goals_line,
            "corners_line": self.corners_line,
            "cards_line": self.cards_line,
        }


class StatsProvider(ABC):
    """Interfaz abstracta. Cada fuente de datos implementa get_match_data()."""

    @property
    @abstractmethod
    def name(self) -> str:
        """Nombre corto del proveedor (para logs y data_quality)."""
        ...

    @abstractmethod
    def get_match_data(
        self,
        home_team: str,
        away_team: str,
        competition: str,
        date: str,
    ) -> Optional[MatchData]:
        """
        Devuelve MatchData normalizado o None si no encuentra el partido.
        Nunca lanza excepciones hacia afuera: atrapa internamente y devuelve None.
        """
        ...

    @abstractmethod
    def health_check(self) -> dict:
        """Retorna estado del proveedor: {ok: bool, message: str, ...}"""
        ...
