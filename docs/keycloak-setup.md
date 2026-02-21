# Keycloak setup (dev)

Guia para levantar y configurar Keycloak para eShopy en desarrollo.

## 1) Docker Compose

Crear archivo `docker-compose.keycloak.yml`:

```yaml
version: '3.8'
services:
  keycloak:
    image: quay.io/keycloak/keycloak:24.0
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres:5432/keycloak
      KC_DB_USERNAME: keycloak
      KC_DB_PASSWORD: keycloak
      KC_HOSTNAME: localhost
      KC_HOSTNAME_PORT: 8080
      KC_HTTP_ENABLED: true
    ports:
      - "8080:8080"
    command: start-dev
    depends_on:
      - postgres

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: keycloak
      POSTGRES_USER: keycloak
      POSTGRES_PASSWORD: keycloak
    ports:
      - "5432:5432"
    volumes:
      - keycloak_db:/var/lib/postgresql/data

volumes:
  keycloak_db:
```

Levantar servicios:

```bash
docker compose -f docker-compose.keycloak.yml up -d
```

## 2) Importar realm recomendado

Se incluye un export listo para desarrollo en:

- `Documentation/Keycloak/realm-eshopy.json`

Importar en startup:

```bash
docker run --rm -v %cd%/Documentation/Keycloak:/opt/keycloak/data/import quay.io/keycloak/keycloak:24.0 start-dev --import-realm
```

Si prefieres import manual desde UI: Realm settings > Action > Partial import.

## 3) Configuracion manual (alternativa)

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

## 4) Claim de permisos (requerido por backend)

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

## 5) Config backend

En `EShopy.Api/appsettings.Development.json`:

- `Keycloak:Authority = http://localhost:8080/realms/eshopy`
- `Keycloak:Audience = eshopy-api`
- El `access_token` debe incluir `aud = eshopy-api`.

## 6) Token de prueba (Postman)

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

## 7) Troubleshooting 401 invalid audience

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
