# Frontend CURRENT_STATE — Estado actual

> Snapshot al 2026-02-19. **El frontend no está iniciado.** No existe ningún código Angular aún.

---

## Estado por aplicación

| App | Estado | Notas |
|---|---|---|
| `apps/admin` | ❌ No iniciado | Por crear desde cero |
| `apps/storefront` | ❌ No iniciado | Por crear desde cero |
| `libs/ui` | ❌ No iniciado | Componentes compartidos por crear |

---

## Estado por feature

| Feature | App | Estado | Backend listo |
|---|---|---|---|
| Design system / tokens | Ambas | ❌ | — |
| AuthInterceptor (JWT) | Admin | ❌ | ✅ Keycloak configurado |
| ErrorInterceptor | Ambas | ❌ | ✅ ErrorResponse estándar |
| Catálogo (admin) | Admin | ❌ | ✅ Endpoints Products |
| Catálogo (storefront) | Storefront | ❌ | ✅ Endpoints Public Products |
| Store info / theming | Storefront | ❌ | ⚠️ Skeleton (datos hardcodeados) |
| Carrito | Storefront | ❌ | ❌ Fase 6 backend |
| Checkout | Storefront | ❌ | ❌ Fase 7 backend |
| Pagos (redirect) | Storefront | ❌ | ❌ Fase 8 backend |
| Pedidos (admin) | Admin | ❌ | ❌ Fase 7 backend |
| Onboarding (tenant) | Admin/Public | ❌ | ❌ Fase 4 backend |
| Configuración del store | Admin | ❌ | ❌ Fase 4 backend |

---

## Lo que el backend ya expone (frontend puede consumir YA)

```
✅ GET  /api/store                          → StorePublicDto (datos hardcodeados en dev)
✅ GET  /api/public/products                → PagedResult<ProductPublicDto>
✅ GET  /api/public/products/{slug}         → ProductPublicDto
✅ POST /api/products                       → ProductAdminDto  [requiere CatalogWrite]
✅ GET  /api/products                       → PagedResult<ProductAdminDto> [requiere CatalogWrite]
✅ GET  /api/products/{id}                  → ProductAdminDto  [requiere CatalogWrite]
✅ PUT  /api/products/{id}                  → ProductAdminDto  [requiere CatalogWrite]
✅ PATCH /api/products/{id}/status          → ProductAdminDto  [requiere CatalogWrite]
```

**Nota dev**: El backend corre en `localhost:5xxx` (verificar puerto al levantar). Keycloak en `localhost:8080`.

---

## Decisiones de arquitectura pendientes de confirmar

| Decisión | Opciones | Recomendación |
|---|---|---|
| Workspace manager | Angular CLI multi-project vs Nx | Nx (mejor para monorepo con libs) |
| Comunicación admin→storefront | Ninguna en MVP | — |
| State management | Signals (decisión firme) | Signals + servicios |
| CSS approach | SCSS + CSS variables | Definido en GOVERNANCE.md |
| Testing | Jest + ATL | Por configurar |
| i18n | No en MVP | Strings directos en español |

---

## Próximo paso recomendado

**Fase F1 — Scaffolding** (ver `BACKLOG.md §Fase F1`):

```bash
# 1. Crear workspace Nx
npx create-nx-workspace@latest eshopy-frontend --preset=angular-monorepo

# 2. Generar apps
nx generate @nx/angular:application admin --routing --style=scss
nx generate @nx/angular:application storefront --routing --style=scss

# 3. Generar lib de UI
nx generate @nx/angular:library ui --directory=libs/ui

# 4. Instalar dependencias base
npm install keycloak-angular keycloak-js
```

Luego implementar los tokens CSS y el primer componente `AppButton` como base del design system.

---

## Variables de entorno necesarias (por configurar)

```typescript
// apps/admin/src/environments/environment.ts
export const environment = {
  apiUrl: 'http://localhost:5000',          // URL del backend
  keycloakUrl: 'http://localhost:8080',     // URL de Keycloak
  keycloakRealm: 'eshopy',
  keycloakClientId: 'eshopy-admin'          // cliente a crear en Keycloak
};

// apps/storefront/src/environments/environment.ts
export const environment = {
  apiUrl: 'http://localhost:5000',
  cartTokenKey: 'eshopy_cart_token'         // key en localStorage
};
```
