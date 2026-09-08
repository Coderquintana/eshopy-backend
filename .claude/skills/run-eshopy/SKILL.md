---
name: run-eshopy
description: Levantar el stack local de eShopy (Docker + API .NET + Storefront Angular) y verificar que responde con datos reales. Usar cuando pidan correr, levantar, arrancar o ver la app / el front / el storefront / el backend.
---

# Levantar eShopy en local

Los dos repos son hermanos y **no** hay un repo padre:

- `eshopy-backend/` — API .NET 10 (este repo)
- `eshopy-frontend/` — workspace Angular 22 (`projects/storefront`, `projects/admin`)

Verificado end-to-end el 2026-09-08. Storefront (catálogo, carrito, checkout) y Admin
(login Keycloak, productos, pedidos) completos — ver `agents/backend/CURRENT_STATE.md`
para el estado real antes de asumir que algo falta.

---

## 1. Infraestructura (Docker)

```bash
docker compose up -d
until docker compose ps --format "{{.Service}} {{.Status}}" | grep -q "sqlserver.*healthy"; do sleep 3; done
```

SQL Server (`localhost:1433`) y Keycloak (`localhost:8080`). El storefront no necesita
Keycloak — solo usa endpoints anonimos — pero el Admin y cualquier endpoint con policy si.

## 2. Migraciones al dia (obligatorio)

```bash
dotnet ef database update --project EShopy.Infrastructure --startup-project EShopy.Api
```

**No es opcional**: B-02 hace que la API tire `InvalidOperationException` al arrancar en
Development si detecta migraciones pendientes. Si salteas esto, el fallo parece un error
de arranque cualquiera.

## 3. API — puerto 5080, NO el 5000 por defecto

```bash
dotnet run --project EShopy.Api --urls http://localhost:5080
```

`launchSettings.json` dice 5000/5001, pero **el proxy del storefront apunta a 5080**
(`eshopy-frontend/projects/storefront/proxy.conf.json`). Arrancar en 5000 deja el front
sin backend, con errores de red que no explican la causa.

Esperar a que responda:

```bash
until curl -s -o /dev/null -w "%{http_code}" http://localhost:5080/health | grep -q 200; do sleep 2; done
```

## 4. Storefront

```bash
cd ../eshopy-frontend && npx ng serve storefront --port 4200
```

El Admin es `--port 4201`, con login Keycloak (PKCE), productos y pedidos ya
funcionando. Necesita su propio proxy (`projects/admin/proxy.conf.json`, mismo
truco de Host que el storefront) y Keycloak realmente arriba para el login real.

---

## El header Host: la trampa principal

`proxy.conf.json` reescribe el `Host` a `smoketest2`:

```json
{ "/api": { "target": "http://localhost:5080", "changeOrigin": false, "headers": { "Host": "smoketest2" } } }
```

No esta ahi para evitar CORS. **El backend resuelve el tenant por el Host**, y `localhost`
no es ninguna tienda: sin esa reescritura todo endpoint publico responde `TENANT_NOT_FOUND`.

Ese tenant tiene que existir y estar `Active` en la DB del contenedor. Verificar:

```bash
MSYS_NO_PATHCONV=1 docker exec eshopy-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'EShopy_Dev_2026!' -C -d EShopy.Dev \
  -Q "SET NOCOUNT ON; SELECT Subdomain, Status FROM Tenants;"
```

`Status = 1` es Active. Si no hay ninguno, el atajo mas rapido sin Keycloak es
`dotnet run --project EShopy.Api -- seed` (crea `smoketest2`, activo, con 3 productos —
ver "Probar el storefront sin Keycloak" en el README del repo). El camino real pasa por
`POST /api/onboarding/tenants` + activacion SUPERADMIN (ver `docs/keycloak-setup.md`).
Para probar otra tienda, cambiar el Host en `proxy.conf.json`.

> `MSYS_NO_PATHCONV=1` es necesario en Git Bash: sin eso convierte `/opt/mssql-tools18/...`
> a una ruta de Windows y `docker exec` falla con "no such file or directory".

---

## Verificar que anda de verdad

No alcanza con que los dos procesos arranquen. Probar el proxy completo:

```bash
curl -s "http://localhost:4200/api/public/products?page=1&pageSize=5"
```

Tiene que devolver productos reales. Si responde `TENANT_NOT_FOUND`, el problema es el
Host (ver arriba); si no responde nada, la API no esta en 5080.

Chequear tambien que el bundle compilo:

```bash
curl -s -o /dev/null -w "%{http_code} %{size_download}\n" http://localhost:4200/main.js
```

Angular renderiza en cliente, asi que `curl` del `index.html` no muestra el catalogo —
para ver la pantalla hace falta un navegador (`Start-Process "http://localhost:4200"` en
PowerShell abre el del usuario).

---

## Que hay para navegar

| Ruta | Que es |
|---|---|
| `/productos` | Catalogo publico (raiz por redirect) |
| `/productos/:slug` | Detalle de producto |
| `/carrito`, `/checkout`, `/checkout/exito` | Flujo de compra completo |
| `/login`, `/productos`, `/pedidos`, `/pedidos/:id` (Admin, :4201) | Panel de la dueña |

Estado real y que falta: `../eshopy-frontend/agents/BACKLOG.md`.

## Bajar todo

```bash
taskkill //F //IM dotnet.exe   # Git Bash: doble slash
taskkill //F //IM node.exe
docker compose down            # opcional, los contenedores pueden quedar arriba
```
