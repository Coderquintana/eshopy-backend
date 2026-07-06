"""
Configuración de SQLAlchemy + SQLite.
Crea las tablas si no existen. 0 configuración: un solo archivo .db.
"""
from __future__ import annotations

from sqlalchemy import create_engine
from sqlalchemy.orm import DeclarativeBase, Session, sessionmaker

from app.core.config import DATABASE_URL
from app.core.logger import get_logger

log = get_logger(__name__)


class Base(DeclarativeBase):
    pass


engine = create_engine(
    DATABASE_URL,
    connect_args={"check_same_thread": False},  # necesario para SQLite en Flask
    echo=False,
)

SessionLocal = sessionmaker(bind=engine, autocommit=False, autoflush=False)


def init_db():
    """Crea todas las tablas si no existen."""
    # Importar modelos para que SQLAlchemy los registre en Base.metadata
    import app.storage.models_orm  # noqa: F401
    Base.metadata.create_all(engine)
    log.info("Base de datos inicializada: %s", DATABASE_URL)


def get_session() -> Session:
    """Retorna una sesión de base de datos."""
    return SessionLocal()
