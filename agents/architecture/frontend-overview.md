# Architecture — Frontend Overview

> Dos aplicaciones Angular 18+ independientes: Admin Panel y Storefront.

## Aplicaciones

| App | URL dev | Propósito | Audiencia |
|---|---|---|---|
| `eShopy Admin` | `localhost:4200` | Panel de gestión del tenant | Tenant Owner/Admin/Staff |
| `eShopy Storefront` | `localhost:4201` | Tienda pública del tenant | Buyers (anónimos en MVP) |

## Principios de diseño

- **Theming centralizado**: tokens CSS en `:root`. Personalización por tenant inyectada al cargar.
- **Mobile-first en Storefront**: la mayoría de compradores en Paraguay usa móvil.
- **Desktop-first en Admin**: optimizado para gestión, responsive como secundario.
- **UI Library compartida** (`libs/ui`): componentes reutilizables entre ambas apps.

## Theming por tenant

```css
/* Variables CSS globales — nunca hardcodear colores */
:root {
  --color-primary: #007bff;       /* Sobreescrito por Store.PrimaryColor */
  --color-background: #ffffff;    /* Sobreescrito por Store.BackgroundColor */
  --font-family-base: 'Inter', sans-serif;
  --spacing-unit: 8px;
  --border-radius-base: 4px;
}
```

```typescript
// Inyección del tema al cargar la app (Storefront)
// GET /api/store → StorePublicDto { PrimaryColor, LogoUrl, BackgroundColor }
document.documentElement.style.setProperty('--color-primary', store.primaryColor);
```

## Breakpoints responsive

| Nombre | Rango | Uso principal |
|---|---|---|
| `xs` | < 600px | Mobile — crítico en Storefront |
| `sm` | 600–960px | Tablet |
| `md` | 960–1280px | Desktop pequeño |
| `lg` | > 1280px | Desktop amplio — Admin |

## Componentes UI Library (libs/ui)

| Componente | Uso |
|---|---|
| `AppButton` | Botón estándar con variantes |
| `AppTextField` | Input de texto con validación |
| `AppSelect` | Select/dropdown |
| `AppToast` | Notificaciones temporales |
| `AppDialog` | Modal/dialog |
| `AppDataGrid` | Tabla de datos con paginación |
| `AppLoading` | Spinner/skeleton |
| `AppPageLayout` | Layout de página con header/sidebar |

## Routing (Admin)

| Ruta | Módulo | Auth |
|---|---|---|
| `/login` | AuthModule | Pública (redirige a Keycloak) |
| `/dashboard` | DashboardModule | Requiere auth |
| `/products` | CatalogModule | CatalogRead |
| `/products/new` | CatalogModule | CatalogWrite |
| `/products/:id` | CatalogModule | CatalogWrite |
| `/orders` | OrdersModule | OrdersRead |
| `/orders/:id` | OrdersModule | OrdersRead |
| `/store/settings` | StoreModule | StoreWrite |
| `/users` | UsersModule | UsersManage |

## Routing (Storefront)

| Ruta | Módulo | Auth |
|---|---|---|
| `/` | HomeModule | Pública |
| `/products` | CatalogModule | Pública |
| `/products/:slug` | CatalogModule | Pública |
| `/cart` | CartModule | Pública |
| `/checkout` | CheckoutModule | Pública |
| `/checkout/success` | CheckoutModule | Pública |
| `/checkout/failed` | CheckoutModule | Pública |

## Auth en Admin (Keycloak OIDC)

- Flujo: Authorization Code + PKCE
- Cliente Keycloak: `eshopy-admin` (confidential)
- Token storage: memory (no localStorage por seguridad)
- Refresh token: automático via interceptor HTTP
- Guard: `AuthGuard` verifica token válido en rutas protegidas

## Comunicación con backend

```typescript
// Interceptor HTTP base
// - Agrega Authorization: Bearer <token>
// - Agrega X-Correlation-Id (generado si ausente)
// - Maneja 401 → refresh token o logout
// - Maneja 403 → notificación de permisos

// TenantId: NUNCA se envía desde el frontend
// Se resuelve automáticamente en backend por subdominio del host
```

## Estado de implementación

❌ **Frontend no iniciado.** Solo existe el backend. El frontend Angular es trabajo futuro.

Los contratos de API están en `architecture/api-contracts.md`.
