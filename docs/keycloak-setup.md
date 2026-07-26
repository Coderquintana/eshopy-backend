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

Incluye el client scope `client-roles` (mapea `resource_access.<client>.roles` al token, requerido
para que el backend pueda llamar la Keycloak Admin API) asignado por default a `eshopy-api`.

**Paso unico despues del primer `docker compose up -d`** (verificado: Keycloak no otorga roles de
`realm-management` a un service account via el import de realm de forma confiable — hay que
otorgarlos con el admin real del `master` realm una sola vez):

```bash
# 1. Token del admin de Keycloak (master realm, no el realm eshopy)
MASTER_TOKEN=$(curl -s -X POST http://localhost:8080/realms/master/protocol/openid-connect/token \
  -d "grant_type=password" -d "client_id=admin-cli" \
  -d "username=admin" -d "password=admin" | python -c "import json,sys; print(json.load(sys.stdin)['access_token'])")

# 2. IDs internos necesarios
CLIENT_UUID=$(curl -s "http://localhost:8080/admin/realms/eshopy/clients?clientId=eshopy-api" \
  -H "Authorization: Bearer $MASTER_TOKEN" | python -c "import json,sys; print(json.load(sys.stdin)[0]['id'])")
SA_USER_ID=$(curl -s "http://localhost:8080/admin/realms/eshopy/clients/$CLIENT_UUID/service-account-user" \
  -H "Authorization: Bearer $MASTER_TOKEN" | python -c "import json,sys; print(json.load(sys.stdin)['id'])")
RM_UUID=$(curl -s "http://localhost:8080/admin/realms/eshopy/clients?clientId=realm-management" \
  -H "Authorization: Bearer $MASTER_TOKEN" | python -c "import json,sys; print(json.load(sys.stdin)[0]['id'])")

# 3. Otorgar manage-users, view-users, view-realm al service account de eshopy-api
ROLES=$(curl -s "http://localhost:8080/admin/realms/eshopy/clients/$RM_UUID/roles" \
  -H "Authorization: Bearer $MASTER_TOKEN" | python -c "
import json,sys
roles = json.load(sys.stdin)
print(json.dumps([r for r in roles if r['name'] in ('manage-users','view-users','view-realm')]))
")
curl -s -X POST "http://localhost:8080/admin/realms/eshopy/users/$SA_USER_ID/role-mappings/clients/$RM_UUID" \
  -H "Authorization: Bearer $MASTER_TOKEN" -H "Content-Type: application/json" -d "$ROLES"
```

Sin este paso, `POST /api/onboarding/tenants` falla con `EXTERNAL_SERVICE_ERROR` (502) al intentar
crear el usuario Owner en Keycloak.

Si necesitas reimportar despues de un cambio manual en el realm: `docker compose down -v` (borra
los volumenes, incluida esta asignacion de roles — hay que repetir el paso de arriba) y
`docker compose up -d` de nuevo.

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
