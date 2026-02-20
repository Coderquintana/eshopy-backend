# Frontend GOVERNANCE — Reglas no negociables

> Última revisión: 2026-02-19. No modificar sin consenso del equipo.

---

## 1. Validaciones — División de responsabilidades

### El backend valida (SIEMPRE, sin excepción)
- Unicidad: slug, sku, email, subdominio
- Integridad referencial: IDs válidos, relaciones existentes
- Reglas de negocio: transiciones de estado, stock, precios
- Seguridad: autorización, permisos por tenant

### El frontend valida (solo UX, para feedback inmediato)
- `required`: campos obligatorios
- `email`: formato de email (`Validators.email`)
- `minLength` / `maxLength`: longitudes de texto
- `min` / `max`: rangos numéricos simples (ej. precio >= 0)
- `pattern`: formato slug (solo `/^[a-z0-9-]+$/`)

### El frontend NUNCA valida
```typescript
// ❌ PROHIBIDO — esto lo hace el backend
checkSlugUniqueness()
checkSkuUniqueness()
validateStateTransition()
calculateTotal()
checkStockAvailability()
validateEmailNotRegistered()
```

---

## 2. Componentes y estilos

| Regla | Correcto | Incorrecto |
|---|---|---|
| Usar solo `libs/ui` | `<app-button>` | `<button class="btn">` |
| Nunca hardcodear colores | `color: var(--color-primary)` | `color: #007bff` |
| Nunca usar px absolutos para spacing | `margin: var(--space-4)` | `margin: 16px` |
| Breakpoints via mixin/variable | `@include mobile { }` | `@media (max-width: 600px)` inline |
| Theming por tenant | variables CSS en `:root` | estilos inline desde TS |

---

## 3. API y autenticación

```typescript
// ✅ Correcto — bearer en header via interceptor (automático)
this.http.get('/api/products')

// ❌ Incorrecto — nunca manualmente
this.http.get('/api/products', { headers: { 'X-Tenant-Id': tenantId } })

// ✅ TenantId NUNCA se envía — el backend lo resuelve por subdominio del host
// ✅ CartToken se envía en header, nunca en body
headers: { 'X-Cart-Token': cartToken }
```

| Regla | Descripción |
|---|---|
| `Authorization: Bearer` | Obligatorio en todos los endpoints admin (automático via interceptor) |
| TenantId | Nunca en body, headers manuales ni query params |
| CartToken | Header `X-Cart-Token`. Generado como UUID en localStorage del Storefront |
| Paginación | Siempre `?page=1&pageSize=20`. Nunca cargar todo |
| Fechas | ISO 8601 UTC al enviar. Formatear en local al mostrar |

---

## 4. Manejo de errores

Toda llamada HTTP que falle debe mapearse a un mensaje amigable. Ver `workflows/error-handling.md`.

```typescript
// Estructura ErrorResponse del backend
interface ErrorResponse {
  traceId: string;
  code: string;        // usar este campo para lógica
  message: string;     // NO mostrar directamente al usuario
  details?: Record<string, unknown>;
}

// ✅ Mapear code → mensaje amigable en español
// ❌ Nunca mostrar error.message directamente
```

| Código backend | Qué mostrar al usuario |
|---|---|
| `VALIDATION_ERROR` | Mensajes por campo del `details` o "Datos inválidos, revisá el formulario." |
| `NOT_FOUND` | "No encontramos lo que buscás." |
| `CONFLICT` | "Ya existe un elemento con esos datos." |
| `UNAUTHORIZED` | Redirigir a login |
| `FORBIDDEN` | "No tenés permisos para esta acción." |
| `PRODUCT_NOT_AVAILABLE` | "Este producto no está disponible." |
| `GENERIC_ERROR` | "Ocurrió un error inesperado. Intentá más tarde." |

---

## 5. Responsive y theming

### Breakpoints (usar variables/mixins — nunca hardcodear)

```scss
// variables.scss
$bp-xs: 600px;
$bp-sm: 960px;
$bp-md: 1280px;

// Uso
@media (max-width: #{$bp-xs}) { /* mobile */ }
```

| App | Estrategia | Prioridad |
|---|---|---|
| Storefront | Mobile-first | xs crítico — mayoría de compradores en Paraguay usa móvil |
| Admin | Desktop-first | lg/md óptimo, responsive como secundario |

### Theming por tenant (solo Storefront)

```typescript
// store.service.ts — al inicializar la app
async loadTheme(): Promise<void> {
  const store = await this.http.get<StorePublicDto>('/api/store').toPromise();
  const root = document.documentElement;
  if (store.primaryColor)   root.style.setProperty('--color-primary', store.primaryColor);
  if (store.backgroundColor) root.style.setProperty('--color-background', store.backgroundColor);
  // LogoUrl: setear en componente AppHeader via @Input
}
```

> Admin Panel NO se personaliza por tenant. Usa siempre el tema base de eShopy.

---

## 6. Stack Angular (decisiones firmes)

| Decisión | Regla |
|---|---|
| **Standalone components** | Sin NgModules. Todos los componentes son `standalone: true` |
| **Signals para estado local** | `signal()`, `computed()`, `effect()`. Sin RxJS para estado local simple |
| **RxJS solo para HTTP** | `HttpClient` retorna Observables. Convertir a Signals con `toSignal()` |
| **Reactive Forms** | Para formularios con validación. Sin template-driven forms |
| **Lazy loading** | Todas las rutas cargan módulos lazy via `loadComponent` / `loadChildren` |
| **No NgRx en MVP** | Signals + servicios son suficientes. NgRx solo si escala a Plan Gold |
| **ChangeDetection.OnPush** | Obligatorio en todos los componentes de `libs/ui` |

---

## 7. Estructura de archivos (convenciones)

```
apps/
  admin/
    src/app/
      features/
        catalog/
          catalog.routes.ts
          product-list/
            product-list.component.ts
            product-list.component.html
            product-list.component.scss
          product-form/
            product-form.component.ts
        orders/
        settings/
      core/
        services/
          auth.service.ts
          api.service.ts
        interceptors/
          auth.interceptor.ts
          error.interceptor.ts
        guards/
          auth.guard.ts
  storefront/
    src/app/
      features/
        home/
        catalog/
        cart/
        checkout/
      core/
        services/
          store.service.ts
          cart.service.ts

libs/
  ui/
    src/
      button/app-button.component.ts
      text-field/app-text-field.component.ts
      ...
```

---

## 8. Convenciones de código

| Ámbito | Regla |
|---|---|
| Idioma del código | Inglés (clases, métodos, propiedades) |
| Idioma de templates/labels | Español (texto visible al usuario) |
| Nombres de archivos | `kebab-case.component.ts` |
| Nombres de clases | `PascalCase` |
| Interfaces | `PascalCase` sin prefijo `I` |
| Commits | `type(scope): resumen` — ej. `feat(catalog): add product form` |
