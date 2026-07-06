"""
Cache en disco con TTL.
Todo request HTTP a proveedores externos pasa por aquí para respetar rate limits.
Se usa un archivo JSON por clave dentro de CACHE_DIR.
"""
import json
import os
import time
from pathlib import Path
from typing import Any, Optional

from app.core.config import CACHE_DIR
from app.core.logger import get_logger

log = get_logger(__name__)
_cache_dir = Path(CACHE_DIR)


def _key_path(key: str) -> Path:
    # Sanitizar la clave para usarla como nombre de archivo
    safe = "".join(c if c.isalnum() or c in "-_" else "_" for c in key)
    return _cache_dir / f"{safe}.json"


def get(key: str) -> Optional[Any]:
    """Devuelve el valor cacheado si no expiró, None si no existe o expiró."""
    path = _key_path(key)
    if not path.exists():
        return None
    try:
        with open(path, "r", encoding="utf-8") as f:
            entry = json.load(f)
        if time.time() > entry["expires_at"]:
            path.unlink(missing_ok=True)
            log.debug("Cache MISS (expirado): %s", key)
            return None
        log.debug("Cache HIT: %s", key)
        return entry["data"]
    except Exception as exc:
        log.warning("Error leyendo cache '%s': %s", key, exc)
        return None


def set(key: str, data: Any, ttl: int) -> None:
    """Guarda data en cache con TTL en segundos."""
    _cache_dir.mkdir(parents=True, exist_ok=True)
    path = _key_path(key)
    entry = {"data": data, "expires_at": time.time() + ttl}
    try:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(entry, f)
        log.debug("Cache SET: %s (TTL=%ds)", key, ttl)
    except Exception as exc:
        log.warning("Error escribiendo cache '%s': %s", key, exc)


def invalidate(key: str) -> None:
    _key_path(key).unlink(missing_ok=True)


def clear_all() -> int:
    """Elimina todas las entradas de cache. Retorna el número eliminado."""
    if not _cache_dir.exists():
        return 0
    count = 0
    for f in _cache_dir.glob("*.json"):
        f.unlink(missing_ok=True)
        count += 1
    return count


def stats() -> dict:
    """Devuelve estadísticas del estado del cache."""
    if not _cache_dir.exists():
        return {"entries": 0, "expired": 0, "valid": 0}
    entries = list(_cache_dir.glob("*.json"))
    now = time.time()
    valid = 0
    expired = 0
    for p in entries:
        try:
            with open(p, "r", encoding="utf-8") as f:
                e = json.load(f)
            if now <= e["expires_at"]:
                valid += 1
            else:
                expired += 1
        except Exception:
            expired += 1
    return {"entries": len(entries), "expired": expired, "valid": valid}
