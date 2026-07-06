"""
Fábrica de la aplicación Flask.
Swagger incluido vía flasgger (no estaba en el plan original de Opus).
"""
from __future__ import annotations

from flasgger import Swagger
from flask import Flask

from app.core.logger import get_logger
from app.storage.db import init_db

log = get_logger(__name__)

SWAGGER_CONFIG = {
    "headers": [],
    "specs": [
        {
            "endpoint": "apispec_1",
            "route": "/apispec_1.json",
            "rule_filter": lambda rule: True,
            "model_filter": lambda tag: True,
        }
    ],
    "static_url_path": "/flasgger_static",
    "swagger_ui": True,
    "specs_route": "/docs/",
    "title": "Match Predictor API",
    "uiversion": 3,
}

SWAGGER_TEMPLATE = {
    "info": {
        "title": "Match Predictor API",
        "description": (
            "Motor de predicción probabilística de partidos de fútbol. "
            "Calcula probabilidades para múltiples mercados (resultado, goles, córners, tarjetas, goleadores) "
            "usando modelos de Poisson bivariado con corrección Dixon-Coles. "
            "Detecta valor comparando probabilidades del modelo vs cuotas de las casas. "
            "Incluye bucle de aprendizaje: registra predicciones y resultados reales para calibrar el modelo.\n\n"
            "**Naturaleza del proyecto:** laboratorio de análisis probabilístico. "
            "El valor está en predecir → registrar → medir el error → ajustar los pesos."
        ),
        "version": "1.0.0",
        "contact": {"name": "Match Predictor"},
    },
    "tags": [
        {"name": "Predicción", "description": "Generar y listar predicciones"},
        {"name": "Aprendizaje", "description": "Registrar resultados y analizar patrones"},
        {"name": "Sistema", "description": "Health check y utilidades"},
    ],
    "basePath": "/",
    "schemes": ["http"],
}


def create_app() -> Flask:
    app = Flask(__name__)

    # Inicializar base de datos
    init_db()

    # Registrar rutas
    from app.api.routes import bp
    app.register_blueprint(bp)

    # Swagger UI en /docs/
    Swagger(app, config=SWAGGER_CONFIG, template=SWAGGER_TEMPLATE)

    log.info("App Flask lista. Swagger en /docs/")
    return app
