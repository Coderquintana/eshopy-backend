"""
Modelos ORM (SQLAlchemy).
Tablas: predictions, outcomes, weight_versions, data_cache.
"""
from __future__ import annotations

import uuid
from datetime import datetime, timezone

from sqlalchemy import JSON, Boolean, Column, DateTime, Float, Integer, String, Text

from app.storage.db import Base


def _utcnow():
    return datetime.now(timezone.utc)


class Prediction(Base):
    __tablename__ = "predictions"

    id = Column(String(36), primary_key=True, default=lambda: str(uuid.uuid4()))
    created_at = Column(DateTime, default=_utcnow)

    home_team = Column(String(100), nullable=False)
    away_team = Column(String(100), nullable=False)
    competition = Column(String(100))
    match_date = Column(String(20))

    # JSON con el request completo
    request_payload = Column(JSON)
    # JSON con legs_ranked completo
    legs_ranked = Column(JSON)
    # JSON con suggested_combo
    suggested_combo = Column(JSON)
    # JSON con value_alerts
    value_alerts = Column(JSON)
    # JSON con data_quality
    data_quality = Column(JSON)

    # Flag para saber si ya se registró el resultado
    result_recorded = Column(Boolean, default=False)


class Outcome(Base):
    __tablename__ = "outcomes"

    id = Column(Integer, primary_key=True, autoincrement=True)
    prediction_id = Column(String(36), nullable=False)
    recorded_at = Column(DateTime, default=_utcnow)

    # JSON: {"match_result": true, "corners_over": false, ...}
    outcomes = Column(JSON)
    final_score = Column(String(20))
    actual_corners = Column(Integer)
    actual_cards = Column(Integer)

    # Cuántas patas acertaron del suggested_combo
    combo_hits = Column(Integer)
    combo_total = Column(Integer)


class WeightVersion(Base):
    __tablename__ = "weight_versions"

    id = Column(Integer, primary_key=True, autoincrement=True)
    created_at = Column(DateTime, default=_utcnow)
    market = Column(String(50), nullable=False)
    version = Column(Integer, default=1)
    weights = Column(JSON, nullable=False)
    note = Column(Text)


class DataCache(Base):
    """Caché de datos externos en base de datos (alternativo al caché en disco)."""
    __tablename__ = "data_cache"

    key = Column(String(200), primary_key=True)
    data = Column(JSON)
    expires_at = Column(Float)  # Unix timestamp
