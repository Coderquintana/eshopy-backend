"""
Configuración global. Lee variables de entorno desde .env.
Nunca hardcodear keys en el código.
"""
import os
from dotenv import load_dotenv

load_dotenv()

API_FOOTBALL_KEY: str = os.getenv("API_FOOTBALL_KEY", "")
FOOTBALL_DATA_KEY: str = os.getenv("FOOTBALL_DATA_KEY", "")
GROQ_API_KEY: str = os.getenv("GROQ_API_KEY", "")
GEMINI_API_KEY: str = os.getenv("GEMINI_API_KEY", "")

DATABASE_URL: str = os.getenv("DATABASE_URL", "sqlite:///predictor.db")
CACHE_DIR: str = os.getenv("CACHE_DIR", ".cache")
LOG_LEVEL: str = os.getenv("LOG_LEVEL", "INFO")
PORT: int = int(os.getenv("PORT", "5000"))

# Diferencia mínima (modelo - casa) para emitir una alerta de valor
VALUE_ALERT_THRESHOLD: float = float(os.getenv("VALUE_ALERT_THRESHOLD", "0.08"))

# TTLs de cache en segundos por tipo de dato
CACHE_TTL = {
    "h2h": 86400 * 3,      # H2H cambia lento: 3 días
    "standings": 86400,    # Tabla: 1 día
    "fixtures": 3600,      # Fixtures: 1 hora
    "odds": 300,           # Cuotas: 5 minutos
    "lineups": 300,        # Lineups: 5 minutos
    "statistics": 3600,    # Stats del partido: 1 hora
    "team_info": 86400 * 7,  # Info de equipo (estadio): 7 días
}
