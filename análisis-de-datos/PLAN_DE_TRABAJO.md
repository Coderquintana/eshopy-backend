# Plan de Trabajo — Motor de Predicción de Partidos (Backend)

> Documento para el agente ejecutor. Objetivo: construir un backend en Python
> (Flask) que reciba un partido, reúna estadísticas de fuentes gratuitas, calcule
> la probabilidad de múltiples "patas" (mercados de apuesta), las rankee de mayor
> a menor probabilidad, compare contra las cuotas de las casas para detectar
> "valor", y devuelva todo en un JSON consumible desde Postman.
>
> **Naturaleza del proyecto:** herramienta de análisis y aprendizaje. Se registra
> cada predicción y su resultado real para iterar sobre los pesos y descubrir
> patrones. NO es un sistema para "ganarle a la casa"; es un laboratorio de
> probabilidades deportivas. Ver sección 0.

---

## 0. Advertencia de diseño (leer antes de codear)

Las combinadas de muchas patas multiplican el error. Si cada pata acierta el 80 %
(muy optimista para props como córners o tapadas), diez patas juntas dan
`0.8^10 ≈ 10.7 %` de que salgan todas. El backend debe **exponer esta matemática
al usuario**, no ocultarla: cada respuesta incluye la probabilidad combinada real
de la selección elegida. El valor del proyecto está en el bucle
predecir → registrar → medir error → ajustar, no en la promesa de ganancia.

Los mercados NO son igual de predecibles. El motor los trata en tres niveles de
confianza (ver sección 4). Mercados de "lotería" (primer goleador exacto,
asistente puntual) se calculan pero se marcan con `confidence: "low"` y por
defecto **no** entran en la selección automática de patas.

---

## 1. Stack y arquitectura

**Lenguaje:** Python 3.11+
**Framework web:** Flask (ligero, suficiente para endpoints REST locales).
**Servidor dev:** Flask integrado; para algo más robusto, `waitress` (multiplataforma).

**Patrón general:** arquitectura por capas, desacoplada por interfaces, para poder
cambiar de fuente de datos sin tocar el motor de probabilidad.

```
app/
├── api/                 # Capa Flask: rutas, validación de request, serialización
│   └── routes.py
├── providers/           # Adaptadores de fuentes de datos (1 archivo por fuente)
│   ├── base.py          # Interfaz abstracta StatsProvider
│   ├── api_football.py  # Fuente principal (free tier)
│   ├── football_data.py # Fuente secundaria / fallback
│   └── soccerdata_src.py# xG y stats profundos (FBref/Sofascore vía lib)
├── models/              # Motor de probabilidad (nada de HTTP acá)
│   ├── poisson.py       # Goles, córners, tarjetas (Poisson / Dixon-Coles)
│   ├── markets.py       # Cada "pata": función que devuelve prob 0..1
│   └── weights.py       # Tabla de pesos por mercado (sección 5)
├── context/             # Capa de contexto cualitativo (IA opcional)
│   ├── news.py          # Lesiones/ánimo desde texto
│   └── value_alert.py   # Comparación prob propia vs cuota casa
├── storage/             # Persistencia de predicciones y resultados
│   ├── db.py            # SQLite (SQLAlchemy)
│   └── models_orm.py
├── core/
│   ├── config.py        # Lee .env (API keys)
│   ├── cache.py         # Cache en disco para no quemar el rate limit
│   └── logging.py
└── main.py              # Crea la app Flask

tests/
requirements.txt
.env.example
```

**Por qué así:** el motor de probabilidad (`models/`) no sabe de dónde vienen los
datos. Recibe un objeto `MatchData` ya normalizado. Si mañana cambia la API, se
reescribe sólo el provider correspondiente. Esto también permite testear el motor
con datos falsos (fixtures) sin llamar a la red.

---

## 2. Fuentes de datos gratuitas (verificadas)

| Fuente | Qué da | Coste | Notas |
|---|---|---|---|
| **API-Football** (api-sports.io) | fixtures, resultados, córners, tarjetas, tiros, lineups, lesiones, H2H, cuotas pre-partido | Free: ~100 req/día, sin tarjeta | Fuente principal. Key en header `x-apisports-key`. |
| **football-data.org** | fixtures, tablas, resultados de competiciones top | Free "forever" para top competitions | Fallback y validación cruzada. |
| **soccerdata** (librería Python) | xG, stats profundos de FBref, Sofascore, etc. | Gratis, sin API key (scrapea) | Para props finos y xG. Cachear agresivo; respetar rate. |
| **Cuotas** | vía API-Football (odds endpoint) o scraping ligero | — | NO es fuente de verdad; se usa sólo para la alerta de valor. |

**Regla de oro del rate limit:** todo request a una fuente pasa por `core/cache.py`
con TTL. Datos que cambian lento (H2H, plantillas) se cachean días; cuotas y
lineups, minutos. Con 100 req/día no se puede scrapear en loop.

**Legal/ToS:** preferir siempre las APIs oficiales con su free tier antes que
scrapear HTML. Cuando se use `soccerdata` (que scrapea), respetar sus intervalos y
no paralelizar. Documentar en el README que el uso es personal/educativo.

---

## 3. Contrato de la API (lo que se prueba en Postman)

### `POST /predict`

**Request (mínimo — el usuario sólo identifica el partido):**
```json
{
  "home_team": "Argentina",
  "away_team": "Nigeria",
  "competition": "World Cup",
  "date": "2026-06-20",
  "top_n": 6,
  "include_low_confidence": false,
  "manual_context": {
    "injuries": ["away: portero titular lesionado"],
    "notes": "Argentina ya clasificada, podría rotar"
  }
}
```
`manual_context` es opcional: permite inyectar a mano lo que el scraping no capte
(el "estado anímico", una lesión de último momento, etc.).

**Response (esquema):**
```json
{
  "match": { "home": "Argentina", "away": "Nigeria", "date": "..." },
  "data_quality": {
    "sources_used": ["api_football", "soccerdata"],
    "missing": ["referee_cards_avg"],
    "warnings": ["cuotas no disponibles para este partido"]
  },
  "legs_ranked": [
    {
      "rank": 1,
      "market": "match_result",
      "selection": "Gana Argentina",
      "probability": 0.89,
      "confidence": "high",
      "drivers": [
        {"factor": "fuerza_relativa", "weight": 0.30, "signal": "+"},
        {"factor": "forma_5", "weight": 0.20, "signal": "+"}
      ],
      "bookmaker": {
        "implied_prob": 0.72,
        "decimal_odds": 1.39,
        "value_flag": "VALOR: modelo 89% vs casa 72%"
      }
    },
    {
      "rank": 2,
      "market": "corners_over",
      "selection": "Más de 8.5 córners",
      "probability": 0.83,
      "confidence": "high",
      "drivers": [ "..." ],
      "bookmaker": { "..." }
    }
  ],
  "value_alerts": [
    {
      "match_level": true,
      "message": "La casa paga 4.00 (25%) al favorito por nombre; el modelo estima 41% según forma actual. Posible sobrevaloración histórica.",
      "model_prob": 0.41,
      "bookmaker_prob": 0.25
    }
  ],
  "suggested_combo": {
    "legs": [1, 2, 4],
    "combined_probability": 0.61,
    "note": "Prob. combinada real de las 3 patas seleccionadas"
  },
  "prediction_id": "uuid-para-registrar-el-resultado-despues"
}
```

### `POST /results/{prediction_id}`
Registra el resultado real del partido (qué patas acertaron). Alimenta el bucle de
aprendizaje.
```json
{
  "outcomes": {
    "match_result": true,
    "corners_over": false,
    "cards_over": true
  },
  "final_score": "3-1",
  "actual_corners": 7
}
```

### `GET /patterns`
Devuelve el análisis agregado sobre todas las predicciones registradas: tasa de
acierto por mercado, calibración (cuando digo 80 %, ¿aciertan el 80 %?), y en qué
mercados se falla sistemáticamente. **Este endpoint es el corazón del ejercicio.**

### `GET /health`
Ping simple + estado de fuentes y del cache.

---

## 4. Mercados (patas) y su nivel de confianza

| Mercado | Cómo se modela | Confianza |
|---|---|---|
| Resultado 1X2 / doble oportunidad | Poisson bivariado (Dixon-Coles) sobre goles esperados | **high** |
| Clasifica a siguiente fase | Simulación del grupo/llave con probs de resultado | **high** |
| Over/Under goles | Distribución de goles totales del modelo Poisson | **high** |
| Over/Under córners | Poisson sobre córners esperados (medias ajustadas por rival y favoritismo) | **high** |
| Over/Under tarjetas | Poisson; **peso fuerte del árbitro** | **medium** |
| Empieza ganando X | Prob. de que marque primero (derivada de fuerza + ritmo) | **medium** |
| Total tapadas portero | Correlación con tiros esperados en contra | **medium** |
| Goleador anota (anytime) | Poisson individual: minutos × tasa de gol del jugador | **medium** |
| Primer goleador exacto | Reparto de la prob. anytime entre timing — muy disperso | **low** |
| Asistencia de jugador X | Datos ralos, alta varianza | **low** |

Las patas `low` se calculan pero quedan fuera del `suggested_combo` salvo que el
request pida `include_low_confidence: true`.

---

## 5. Factores y pesos (PUNTO DE PARTIDA — se ajustan con `/patterns`)

Los pesos son **por mercado**, no globales. Lo que predice córners no es lo que
predice tarjetas. Estos valores son la hipótesis inicial; el bucle de aprendizaje
los recalibra.

### 5.1 Resultado / Clasificación
| Factor | Peso |
|---|---|
| Fuerza relativa de plantel (rating tipo Elo o valor de mercado) | 0.30 |
| Forma últimos 5 partidos (ponderada por calidad de rival) | 0.20 |
| Bajas de jugadores clave (lesión/suspensión) | 0.15 |
| Historial directo H2H (con decaimiento temporal) | 0.10 |
| Localía / contexto neutral | 0.10 |
| Importancia y necesidad de resultado (ya clasificado, rotación) | 0.15 |

### 5.2 Córners
| Factor | Peso |
|---|---|
| Córners a favor/en contra por partido (media histórica de ambos) | 0.45 |
| Forma reciente en córners (últimos 5) | 0.25 |
| Favoritismo (el favorito ataca más → más córners) | 0.20 |
| Árbitro y ritmo esperado del partido | 0.10 |

### 5.3 Tarjetas
| Factor | Peso |
|---|---|
| Árbitro asignado (media de tarjetas por partido del árbitro) | 0.35 |
| Media de tarjetas de ambos equipos | 0.30 |
| Rivalidad / importancia del partido | 0.20 |
| Estilo de juego agresivo (faltas cometidas por partido) | 0.15 |

### 5.4 Goles (Over/Under, ambos marcan)
| Factor | Peso |
|---|---|
| xG a favor y en contra de ambos equipos | 0.40 |
| Goles marcados/recibidos forma reciente | 0.25 |
| Fuerza relativa (partidos parejos → menos goles del favorito) | 0.20 |
| Bajas ofensivas/defensivas clave | 0.15 |

### 5.5 Goleador anota (anytime)
| Factor | Peso |
|---|---|
| Tasa de gol por 90 min del jugador (temporada + torneo) | 0.45 |
| Minutos esperados (¿titular? ¿rota?) | 0.25 |
| Fuerza defensiva del rival | 0.20 |
| Penales asignados al jugador | 0.10 |

> **Nota para el agente:** implementar los pesos como diccionarios en
> `models/weights.py`, NO hardcodeados en la lógica. `/patterns` debe poder
> proponer nuevos pesos y guardarlos versionados.

---

## 6. La alerta de valor (el "las casas pagan 4 pero puede ganar")

Lógica en `context/value_alert.py`:

1. Convertir cuota decimal de la casa a probabilidad implícita:
   `implied = 1 / decimal_odds` (y normalizar quitando el margen/overround de la casa).
2. Comparar con la probabilidad del modelo.
3. Si `model_prob - implied_prob > umbral` (ej. 0.08), emitir alerta de VALOR.
4. Caso especial "favorito por historia": si el equipo tiene baja forma reciente
   (`forma_5` pobre) pero cuota muy baja (la casa lo hace mega favorito), y el
   modelo discrepa, marcar `historical_overvaluation: true` con un mensaje
   explicativo. Esto es exactamente el patrón que describiste: nombre e historia
   inflando la cuota mientras el presente dice otra cosa.

**Importante:** la cuota NUNCA entra como input del cálculo de probabilidad. Es
sólo el espejo contra el cual se compara. Si se filtrara al modelo, éste sólo
copiaría a la casa (que es justo lo que se quiere evitar).

---

## 7. Capa de IA (opcional, gratuita)

Uso acotado y sólo donde aporta:

- **Extracción de contexto cualitativo:** dado texto (noticias, `manual_context`),
  convertir "el portero titular está lesionado y el equipo viene de tres derrotas"
  en señales estructuradas (`{injury: keeper, morale: low}`) que ajustan pesos.
- **Redacción del mensaje de la alerta de valor** en lenguaje natural.

**Opciones gratuitas:** Groq (free tier generoso, muy rápido), Google Gemini free
tier, o modelos locales vía Ollama (0 coste, corre en la máquina). La IA se aísla
detrás de una interfaz `ContextEnricher` para poder cambiar de proveedor o
desactivarla sin romper nada. **El sistema debe funcionar sin IA** (degradación
elegante): si no hay key, se usa sólo `manual_context`.

La IA **no calcula probabilidades**. Sólo estructura contexto y redacta. Las
probabilidades salen siempre de los modelos estadísticos, que son auditables.

---

## 8. Persistencia y bucle de aprendizaje

- **SQLite** vía SQLAlchemy (0 configuración, un archivo).
- Tablas: `predictions` (input + legs calculadas + prob), `outcomes` (resultado
  real), `weight_versions` (historial de pesos), `data_cache`.
- Flujo del ejercicio:
  1. `POST /predict` → guarda predicción con `prediction_id`.
  2. Se juega el partido.
  3. `POST /results/{id}` → guarda qué pasó.
  4. `GET /patterns` → calcula, por mercado: aciertos/total, **curva de
     calibración** (clave: ver si las probabilidades están bien ajustadas), y
     sesgos ("en córners over siempre sobreestimamos").
  5. Ajustar pesos en `weights.py`, versionar, repetir.

**Métrica estrella:** calibración, no sólo % de acierto. Un modelo que dice 60 % y
acierta el 60 % de las veces es honesto y útil. Uno que dice 90 % y acierta 60 %
está roto aunque "acierte bastante".

---

## 9. Orden de implementación (para el agente)

1. **Esqueleto Flask** + `/health` + config + `.env.example`. Que levante.
2. **Provider API-Football** con cache. Endpoint interno para traer un fixture y
   sus stats. Probar en Postman que trae datos reales.
3. **Normalización** a `MatchData` (dataclass): todo lo que el motor necesita, en
   un objeto limpio.
4. **Motor Poisson** para goles y córners + tests con datos fixture.
5. **`markets.py`**: implementar mercados `high` primero (resultado, córners,
   goles). Rankeo por probabilidad.
6. **`POST /predict`** devolviendo `legs_ranked`. Ya es usable en Postman.
7. **Persistencia** + `POST /results` + `GET /patterns` con calibración.
8. **Alerta de valor** (odds endpoint + comparación).
9. **Mercados `medium`/`low`** (tarjetas, goleador, etc.).
10. **Capa de IA** para contexto (último, opcional).

Cada paso debe dejar el sistema funcionando y probable en Postman antes de seguir.

---

## 10. Notas finales para el agente

- Todas las API keys en `.env`, nunca en el código. Entregar `.env.example`.
- `requirements.txt` con versiones fijadas.
- Manejar SIEMPRE el caso "dato faltante": si no hay árbitro asignado, el peso de
  árbitro se redistribuye y se anota en `data_quality.missing`. Nunca inventar
  datos; marcar la incertidumbre.
- Logs claros de qué fuente respondió y cuántos requests quedan del rate limit.
- Zona horaria explícita en fechas (UTC en storage, local en display).
- El código en español o inglés es indistinto, pero consistente. Comentar el
  "por qué" de las fórmulas de probabilidad, no el "qué".

---

## 11. Simulación Monte Carlo para combinadas coherentes

*(Añadido en iteración 2 — feedback de Opus durante implementación)*

### El problema con el top-N individual

Rankear patas por probabilidad individual e ignorar correlaciones produce combinadas
incoherentes. Si "gol de Yamal", "gol de Oyarzabal" y "gol de Cucurella" son las
tres patas más probables, el ranking las propondría juntas — pero eso requiere 3
goles de España en un partido donde el modelo espera 2-1. Multiplicar
`p1 × p2 × p3` como si fueran independientes da probabilidades falsas.

### Solución: `models/simulation.py`

Simular el partido N veces (default 5.000) generando resultados **coherentes** en
cada simulación:

1. Muestrear `home_goals ~ Poisson(λ_home)`, `away_goals ~ Poisson(λ_away)`.
2. Distribuir los goles del equipo entre jugadores via **Multinomial(home_goals, probs_jugadores)**.
   Esto garantiza que la suma de goles individuales = goles del equipo en esa simulación.
3. Muestrear córners y tarjetas de forma independiente (no correlacionan con el marcador).
4. La probabilidad conjunta de una combinada = fracción de simulaciones donde
   **todas** las patas ocurren simultáneamente.

La correlación queda incorporada automáticamente. "Yamal AND Oyarzabal" en un partido
que termina 1-0 es imposible — y la simulación lo refleja.

### Campo `condition` en `LegResult`

Cada pata lleva un dict `condition` estructurado (interno, no serializado):
```python
{"type": "result", "outcome": "home"}
{"type": "over_under", "event": "goals", "line": 2.5, "direction": "over"}
{"type": "btts", "value": True}
{"type": "goalscorer", "player": "Yamal", "team": "home"}
```
Esto permite evaluar cualquier pata contra la simulación sin parsear texto.

### Selección óptima del combo

`find_best_combo()` itera sobre `C(n_candidates, top_n)` combinaciones (por defecto
`C(8, 3) = 56`), descarta las contradictorias (over + under mismo evento, etc.) y
selecciona la de **máxima probabilidad conjunta**. La respuesta incluye:
- `combined_probability`: prob. conjunta real (Monte Carlo)
- `naive_product`: producto ingenuo ignorando correlaciones
- `correlation_discount`: diferencia. Positivo = correlación negativa (patas que
  raramente ocurren juntas). Negativo = correlación positiva (ocurren más juntas de
  lo esperado — ej. "local gana" + "over 2.5 goles").

### numpy como optimización, no como requisito

El módulo detecta numpy en runtime. Si está disponible (máquinas modernas), usa
`np.random.poisson` y `np.random.multinomial` vectorizados. Si no (CPUs sin SSE4.2
que no pueden ejecutar los wheels de numpy 2.x), cae a algoritmo de Knuth + Python
puro. Matemáticamente equivalente, ~10× más lento pero completamente funcional.

---

## 12. Backtest: calibrar con datos históricos antes del torneo

*(Añadido en iteración 2 — feedback de Opus durante implementación)*

### Problema

`GET /patterns` necesita predicciones + resultados acumulados para calibrar. No hay
atajo honesto para el primer día en vivo. Pero sí se pueden cargar partidos ya
jugados (clasificatorias, torneos anteriores) para "pre-calentar" el modelo.

### `POST /backtest`

Mismo pipeline que `POST /predict`, pero recibe `known_result` en el mismo request:

```json
{
  "home_team": "Barcelona",
  "away_team": "Real Madrid",
  "competition": "La Liga",
  "date": "2024-10-26",
  "known_result": {
    "final_score": "4-0",
    "actual_corners": 6,
    "actual_cards": 5
  }
}
```

El endpoint:
1. Corre el pipeline de predicción con datos de API-Football (históricos).
2. Deriva automáticamente los outcomes a partir del marcador conocido.
3. Guarda predicción + outcome en una sola transacción (flag `result_recorded=True`).
4. Retorna la predicción + un análisis de aciertos inmediato por pata.

Esto permite cargar N partidos históricos del clasificatorio CONMEBOL/UEFA antes del
Mundial, y que `GET /patterns` ya tenga datos de calibración desde el día 1.

### Flujo de bootstrap recomendado

```
for partido in clasificatorias_2025[-30:]:
    POST /backtest con resultado conocido
GET /patterns  # ya tiene 30 puntos de calibración
Ajustar pesos en weights.py según sesgos detectados
Empezar Mundial con modelo pre-calibrado
```

---

## 13. Iteración 3 — Batch WC 2022 y corrección del evaluador

*(Sesiones de implementación — julio 2026)*

### 13.1 Script de batch backtest (`scripts/batch_backtest_wc2022.py`)

Se creó un script que corre los 10 partidos verificados del Mundial Qatar 2022
contra `POST /backtest` y al final imprime `GET /patterns` completo.

Partidos incluidos (ordenados para maximizar reutilización de caché de disco):

| # | Partido | Fecha | Score | Nota |
|---|---------|-------|-------|------|
| 1 | Argentina vs Australia | 2022-12-03 | 2-1 | Favorito gana ajustado |
| 2 | Argentina vs Saudi Arabia | 2022-11-22 | 1-2 | SORPRESA |
| 3 | England vs Senegal | 2022-12-04 | 3-0 | Favorito gana cómodo |
| 4 | Spain vs Costa Rica | 2022-11-23 | 7-0 | Goleada extrema |
| 5 | Japan vs Germany | 2022-11-23 | 2-1 | SORPRESA |
| 6 | France vs Denmark | 2022-11-26 | 2-1 | Favorito gana ajustado |
| 7 | Brazil vs Switzerland | 2022-11-28 | 1-0 | Favorito gana mínimo |
| 8 | Portugal vs Uruguay | 2022-11-28 | 2-0 | Favorito gana |
| 9 | Netherlands vs Qatar | 2022-11-29 | 2-0 | Favorito gana cómodo |
| 10 | Morocco vs Croatia | 2022-11-23 | 0-0 | Empate sin goles |

**Fixes al script:**
- Eliminado el `input()` interactivo que generaba `EOFError` en shell no-interactiva.
- Guard de rate modificado a `if 0 <= rate_before < 5:` (solo abortar cuando se
  sabe con certeza que quedan menos de 5 requests; -1 = cuota agotada/error,
  no abortar — continúa desde caché).
- Todos los caracteres fuera de cp1252 reemplazados (`→` por `->`, `—` por `-`,
  `█` por `#`) para compatibilidad con Windows sin `PYTHONIOENCODING=utf-8`.

### 13.2 Bug: filtro de fixture ignoraba el lado home

**Síntoma:** Japan vs Germany (2022-11-23) retornaba `sources: []`. El partido
existe en la API como `Germany vs Japan` (Germany=home, Japan=away). Al buscar por
el ID de Japan y filtrar `away_name contains "Germany"`, el filtro fallaba porque
Germany está en el campo home.

**Fix en `app/providers/api_football.py` — `search_fixture` paso 5:**

```python
# Antes: solo chequeaba el lado away
if fix_date == date and (away_lower in away_name or away_name in away_lower):

# Después: chequea ambos lados
fix_home = teams.get("home", {}).get("name", "").lower()
fix_away = teams.get("away", {}).get("name", "").lower()
other_matches = (away_lower in fix_away or fix_away in away_lower or
                 away_lower in fix_home or fix_home in away_lower)
if fix_date == date and other_matches:
```

También se cambió el fallback de `if not league_id or not fixtures:` a `if True:`
para que el fallback vía equipo visitante siempre se intente si el paso 5 no
encontró fixture.

**Regla:** la API de api-sports.io no garantiza que el equipo solicitado sea el
`home`. Siempre verificar ambos lados del fixture.

### 13.3 Bug crítico: `_check_leg_outcome` — evaluación incorrecta

Este fue el bug más impactante. Causó que `GET /patterns` reportara
`match_result` con **6.2% de acierto** (casi inverso al real).

**Causa raíz — branch `match_result`:**

```python
# Código original (INCORRECTO)
if "gana" in sel and ("local" in sel or "home" in sel):
    return h > a   # solo entraba para "Gana Local"
elif "empate" in sel:
    return h == a
elif "gana" in sel:
    return a > h   # <-- TODAS las "Gana England", "Gana Argentina", etc.
                   #     caían acá → fórmula de victoria visitante
```

El backend genera selecciones como `"Gana England"` (nombre del equipo), nunca
`"Gana Local"`. Entonces todas las victorias del equipo local se evaluaban con
la fórmula equivocada.

**Fix — extraer nombre del equipo de la selección:**

```python
if "gana" in sel:
    team_in_sel = sel.replace("gana", "").strip()
    ht = home_team.lower().strip()
    at = away_team.lower().strip()
    if ht and (team_in_sel in ht or ht in team_in_sel):
        return h > a   # local gana
    if at and (team_in_sel in at or at in team_in_sel):
        return a > h   # visitante gana
    if "local" in sel or "home" in sel:
        return h > a   # fallback legacy
    return None
```

`_check_leg_outcome` ahora recibe `home_team` y `away_team` como parámetros.
Se actualizaron los 3 call sites: `/patterns` (línea ~353), `/backtest` combo
(línea ~649), y `/backtest` per-leg (línea ~692). También `_score_combo` recibe
y propaga estos parámetros.

**Branches faltantes agregados:**

| Branch | Problema anterior | Fix |
|--------|------------------|-----|
| `double_chance` | Retornaba `None` siempre | Agrega lógica `1X` → `h >= a`, `X2` → `a >= h` |
| `goals_over_under "menos de"` | Solo tenía "más de"/"over"; under devolvía `None` | Agrega `"menos de" in sel or "under" in sel: return not (total > line)` |
| `btts` | Retornaba `None` siempre | Agrega `both = h > 0 and a > 0`; `"no" in sel: return not both` |
| `first_to_score` | Implícito `None` | Explícito `return None` (no derivable del score final) |
| `corners_over_under "menos de"` | Solo "más de" | Mismo patrón que goals |
| `cards_over_under "menos de"` | Solo "más de" | Mismo patrón que goals |

Se agregaron helpers internos `_parse_score()` y `_extract_line()` para
evitar duplicar el parsing en cada branch.

### 13.4 Resultados de calibración post-fix (10 partidos WC 2022)

```
match_result      total=19   acierto=94.7%   <- era 6.2% (bug de evaluación)
  pred=0.6  real=100%  n=14  err=0.40  <- underconfident: modelo dice 60%, realidad 100%
  pred=0.7  real=100%  n=3   err=0.30
  pred=0.9  real=100%  n=1   err=0.10

double_chance     total=102  acierto=64.7%
  pred=0.5  real=7%    n=28  err=0.43  <- sobregenera double_chance de baja prob
  pred=0.7  real=93%   n=15  err=0.23
  pred=0.8  real=88%   n=49  err=0.08  <- bien calibrado

goals_over_under  total=54   acierto=61.1%
  pred=0.6  real=50%   n=42  err=0.10  <- ligeramente sobreestima over
  pred=0.7  real=100%  n=11  err=0.30

btts              total=51   acierto=62.7%
  pred=0.5  real=0%    n=12  err=0.50  <- modelo sobreestima btts=yes en partidos WC
  pred=0.6  real=82%   n=39  err=0.22
```

**Hallazgos clave:**
- `match_result`: modelo es **underconfident** (dice 60%, acierta 100% en esta
  muestra). Normal con n=14; a vigilar si persiste con más datos.
- `double_chance` en pred=0.5: el modelo genera demasiadas patas de baja confianza.
  Considerar elevar el umbral mínimo de inclusión.
- `btts` en pred=0.5: el modelo sobreestima ambos equipos marcan en el contexto
  de un Mundial (partidos más cerrados, equipos defensivos).
- `double_chance` en pred=0.8: `err=0.08` — mercado mejor calibrado del batch.

### 13.5 Próximos ajustes sugeridos

1. **Umbral de inclusión de patas:** descartar patas con `probability < 0.55` del
   `suggested_combo` (elimina el ruido del bucket pred=0.5 en double_chance/btts).
2. **Recalibración de btts:** el prior de btts en partidos internacionales neutrales
   debería ser más bajo que en liga doméstica. Considerar factor `btts_neutral_adj`.
3. **Más datos de backtest:** 10 partidos (n pequeño) — cargar 30+ partidos de
   clasificatorias CONMEBOL/UEFA 2025 para curvas de calibración más robustas.
4. **Regularización/shrinkage:** aplicar shrinkage hacia la media global cuando el
   historial de un equipo es escaso (< 5 partidos en caché). Ya implementado en
   `estimate_lambdas_from_stats` con factor configurable.
