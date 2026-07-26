# Keycloak setup (dev)

Guia para levantar y configurar Keycloak para eShopy en desarrollo.

## 0) Levantar todo el entorno local

El entorno local (SQL Server + Keycloak + su propia base Postgres) esta definido en
`docker-compose.yml`, en la raiz del repo. No hace falta copiar comandos a mano.

```bash
docker compose up -d
```

Esto levanta:

- `sqlserver` — SQL Server 2022 en `localhost:1433` (SA / ver `MSSQL_SA_PASSWORD` en el compose)
- `keycloak-db` — Postgres para el storage interno de Keycloak
- `keycloak` — Keycloak en `localhost:8080`, con `--import-realm` importando automaticamente
  `Documentation/Keycloak/realm-eshopy.json` al arrancar (no requiere import manual)

Aplicar migraciones contra el SQL Server del compose:

```bash
dotnet ef database update --project EShopy.Infrastructure --startup-project EShopy.Api
```

Para apagar el entorno (conservando los datos en los volumenes):

```bash
docker compose down
```

## 1) Realm importado automaticamente

El realm `eshopy` se importa solo al arrancar `keycloak` (ver seccion 0). El export vive en:

- `Documentation/Keycloak/realm-eshopy.json`

Incluye ademas el service account de `eshopy-api` con los roles `manage-users`/`view-users`
de `realm-management`, necesarios para que el backend cree usuarios (Owner de un tenant nuevo)
via la Keycloak Admin API durante el onboarding.

Si necesitas reimportar despues de un cambio manual en el realm: `docker compose down -v` (borra
los volumenes) y `docker compose up -d` de nuevo.

## 2) Configuracion manual (alternativa, si no usas el compose)

1. Abrir `http://localhost:8080`
2. Login: `admin / admin`
3. Crear realm `eshopy`
4. Crear client `eshopy-api`
   - Client Protocol: `openid-connect`
   - Access Type: `confidential`
   - Valid Redirect URIs: `http://localhost:4200/*`, `http://localhost:5000/*`
   - Web Origins: `http://localhost:4200`, `http://localhost:5000`
5. Crear roles de realm
   - `ESHOPY_SUPERADMIN`
   - `TENANT_OWNER`
   - `TENANT_ADMIN`
   - `TENANT_STAFF`
6. Crear usuarios de prueba
   - `superadmin@eshopy.local` (rol `ESHOPY_SUPERADMIN`)
   - `owner@tenant1.local` (rol `TENANT_OWNER`)
   - `admin@tenant1.local` (rol `TENANT_ADMIN`)
   - `staff@tenant1.local` (rol `TENANT_STAFF`)

## 3) Claim de permisos (requerido por backend)

Las policies del backend se validan contra claim `permissions`.

Permisos esperados:

- `tenants.read`, `tenants.write`
- `store.read`, `store.write`
- `catalog.read`, `catalog.write`
- `orders.read`, `orders.write`
- `payments.read`
- `users.manage`
- `billing.manage`

Mapeo recomendado por rol:

- `ESHOPY_SUPERADMIN`: todos los permisos
- `TENANT_OWNER`: `store.*`, `catalog.*`, `orders.*`, `payments.read`, `users.manage`, `billing.manage`
- `TENANT_ADMIN`: `store.read`, `catalog.*`, `orders.*`, `payments.read`
- `TENANT_STAFF`: `store.read`, `catalog.read`, `orders.read`

## 4) Config backend

En `EShopy.Api/appsettings.Development.json`:

- `Keycloak:Authority = http://localhost:8080/realms/eshopy`
- `Keycloak:Audience = eshopy-api`
- El `access_token` debe incluir `aud = eshopy-api`.
- `Keycloak:AdminBaseUrl`, `Keycloak:AdminClientId`, `Keycloak:AdminClientSecret` — usados por el
  backend para provisionar el usuario Owner en Keycloak durante el onboarding de un tenant nuevo
  (reutiliza el service account de `eshopy-api`, ver seccion 1).

## 5) Token de prueba (Postman)

Client sugerido para password grant en dev: `eshopy-api`.

Endpoint:

```text
POST http://localhost:8080/realms/eshopy/protocol/openid-connect/token
```

Body (`x-www-form-urlencoded`):

- `grant_type=password`
- `client_id=eshopy-api`
- `client_secret=eshopy-api-secret`
- `username=admin@tenant1.local`
- `password=Admin123!`

Usar `access_token` como `Bearer` en endpoints admin.

## 6) Troubleshooting 401 invalid audience

Si la API responde `401` con `SecurityTokenInvalidAudienceException`:

1. Decodificar el JWT y verificar claim `aud`.
2. Debe contener `eshopy-api`.
3. Si falta, agregar un client scope dedicado para audiencia:
   - Client scope: `audience-eshopy-api`
   - Mapper type: `Audience`
   - Included Client Audience: `eshopy-api`
   - Add to access token: ON
4. Asignar ese client scope como `Default` en el cliente `eshopy-api`.
5. Nota: `eshopy-api-dedicated` puede quedar con `Assigned type = None`; no es necesario cambiarlo.
