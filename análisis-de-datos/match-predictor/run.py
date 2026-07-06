"""
Punto de entrada. Corre el servidor con waitress (multiplataforma) o Flask dev.
"""
import os
import sys

from app.core.config import PORT
from app.core.logger import get_logger
from app.main import create_app

log = get_logger("run")


def main():
    app = create_app()

    use_waitress = "--waitress" in sys.argv or os.getenv("USE_WAITRESS", "").lower() == "true"

    if use_waitress:
        try:
            from waitress import serve
            log.info("Iniciando con waitress en http://0.0.0.0:%d", PORT)
            log.info("Swagger UI: http://localhost:%d/docs/", PORT)
            serve(app, host="0.0.0.0", port=PORT)
        except ImportError:
            log.warning("waitress no instalado, usando Flask dev server.")
            _run_flask_dev(app)
    else:
        _run_flask_dev(app)


def _run_flask_dev(app):
    log.info("Iniciando Flask dev server en http://localhost:%d", PORT)
    log.info("Swagger UI: http://localhost:%d/docs/", PORT)
    app.run(host="0.0.0.0", port=PORT, debug=True)


if __name__ == "__main__":
    main()
