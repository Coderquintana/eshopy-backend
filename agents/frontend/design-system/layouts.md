# Design System — Layouts

> Estructura visual de páginas para Admin Panel y Storefront.

---

## Admin Panel Layout

```
┌─────────────────────────────────────────────────────┐
│  TOPBAR   [Logo eShopy]  [Tenant name]  [User menu] │  height: 64px; fixed
├──────────┬──────────────────────────────────────────┤
│          │                                          │
│ SIDEBAR  │  MAIN CONTENT                            │
│ 240px    │                                          │
│ fixed    │  [Page title]                [Actions]  │
│          │  ──────────────────────────────────────  │
│ [nav]    │  [Content area]                          │
│ [nav]    │                                          │
│ [nav]    │                                          │
│          │                                          │
└──────────┴──────────────────────────────────────────┘
```

```typescript
// Selector: <app-page-layout>
// Archivo: libs/ui/src/page-layout/app-page-layout.component.ts

@Input() app: 'admin' | 'storefront' = 'admin';
@Input() pageTitle = '';
@Input() showBackButton = false;
```

```html
<!-- Uso en cada página del Admin -->
<app-page-layout pageTitle="Catálogo de productos">
  <ng-container slot="actions">
    <app-button routerLink="/products/new">Nuevo producto</app-button>
  </ng-container>

  <!-- Contenido de la página -->
  <app-data-grid [columns]="columns" [rows]="products()" />
</app-page-layout>
```

### AppSidebar — navegación

```html
<!-- Interno a AppPageLayout cuando app="admin" -->
<nav class="sidebar">
  <app-sidebar-item icon="dashboard" label="Dashboard"    routerLink="/dashboard" />
  <app-sidebar-item icon="inventory" label="Catálogo"     routerLink="/products" />
  <app-sidebar-item icon="orders"    label="Pedidos"      routerLink="/orders" />
  <app-sidebar-item icon="store"     label="Mi Tienda"    routerLink="/store/settings" />
  <app-sidebar-item icon="people"    label="Usuarios"     routerLink="/users" />
</nav>
```

### Variables CSS del Admin

```scss
.admin-layout {
  --sidebar-width:     240px;
  --topbar-height:     64px;
  --content-max-width: 1200px;
  --content-padding:   var(--space-8);
}

@include mobile {
  --sidebar-width: 0;  /* sidebar se oculta en mobile → hamburger menu */
}
```

---

## Storefront Layout

```
┌──────────────────────────────────────────────────────┐
│  HEADER  [Logo]  [Nav]  [Cart icon + count]           │  sticky, height: 72px
├──────────────────────────────────────────────────────┤
│                                                      │
│  HERO / BREADCRUMB                                   │
│                                                      │
├──────────────────────────────────────────────────────┤
│                                                      │
│  MAIN CONTENT                                        │
│                                                      │
│  max-width: 1200px; margin: 0 auto; padding: 0 16px  │
│                                                      │
├──────────────────────────────────────────────────────┤
│  FOOTER  [Info tienda]  [Links]  [Copyright]          │
└──────────────────────────────────────────────────────┘
```

```html
<!-- app.component.html (Storefront) -->
<app-storefront-header [cartCount]="cartCount()" />

<main>
  <router-outlet />
</main>

<app-storefront-footer />
<app-toast-container />
```

### AppStorefrontHeader

```typescript
@Input() cartCount = 0;  // actualizado por CartService
// Muestra logo (Store.logoUrl), nombre (Store.name) y badge del carrito
```

```html
<header class="storefront-header">
  <div class="container">
    <a routerLink="/" class="brand">
      <img [src]="storeLogo()" [alt]="storeName()" class="logo" />
      <span>{{ storeName() }}</span>
    </a>

    <nav>
      <a routerLink="/products">Tienda</a>
    </nav>

    <a routerLink="/cart" class="cart-btn">
      <app-icon name="shopping-cart" />
      @if (cartCount > 0) {
        <span class="badge">{{ cartCount }}</span>
      }
    </a>
  </div>
</header>
```

### Variables CSS del Storefront

```scss
.storefront-layout {
  --header-height:     72px;
  --container-width:   1200px;
  --container-padding: var(--space-4);   /* 16px en mobile */

  @include desktop {
    --container-padding: var(--space-8); /* 32px en desktop */
  }
}
```

---

## Product Grid (Storefront)

```
Mobile (1 col)       Tablet (2 cols)      Desktop (3-4 cols)
┌──────────┐         ┌────┐ ┌────┐        ┌───┐ ┌───┐ ┌───┐ ┌───┐
│  Card    │         │Card│ │Card│        │Crd│ │Crd│ │Crd│ │Crd│
├──────────┤         ├────┤ ├────┤        ├───┤ ├───┤ ├───┤ ├───┤
│  Card    │         │Card│ │Card│        │Crd│ │Crd│ │Crd│ │Crd│
└──────────┘         └────┘ └────┘        └───┘ └───┘ └───┘ └───┘
```

```scss
.product-grid {
  display: grid;
  gap: var(--space-4);
  grid-template-columns: 1fr;

  @include tablet  { grid-template-columns: repeat(2, 1fr); }
  @include desktop { grid-template-columns: repeat(3, 1fr); }
  @include wide    { grid-template-columns: repeat(4, 1fr); }
}
```

---

## Checkout Layout (Storefront)

```
Mobile: columna única
Desktop: 60% form | 40% resumen

┌─────────────────────┬─────────────────┐
│  FORMULARIO         │  RESUMEN        │
│  Datos del comprador│  Items del      │
│                     │  carrito        │
│  Dirección de envío │                 │
│                     │  Total: ₲85,000 │
│  [Ir al pago]       │                 │
└─────────────────────┴─────────────────┘
```

```scss
.checkout-layout {
  display: grid;
  gap: var(--space-8);
  grid-template-columns: 1fr;     /* mobile: stacked */

  @include desktop {
    grid-template-columns: 3fr 2fr;  /* desktop: 60/40 */
  }
}
```

---

## Admin — Lista de productos

```
┌─────────────────────────────────────────────────┐
│ [PAGE TITLE: Catálogo]          [+ Nuevo]        │
├─────────────────────────────────────────────────┤
│ [Buscar...] [Estado ▼]          [20 por pág ▼]  │
├────────────────────────────────────────────────-┤
│ Nombre      │ SKU  │ Precio │ Stock │ Estado     │
├─────────────┼──────┼────────┼───────┼────────────┤
│ Remera Blanca│REM-01│₲85,000│  50   │ ● Activo   │
│ Pantalón...  │PAN-01│₲120,000│  20  │ ○ Borrador │
├─────────────────────────────────────────────────┤
│ ← 1  2  3  4  5 →                  150 productos │
└─────────────────────────────────────────────────┘
```

---

## Admin — Formulario de producto

```
┌──────────────────────────────────────────────────┐
│ ← Volver                                         │
│ NUEVO PRODUCTO / EDITAR PRODUCTO                  │
├──────────────────────────────────────────────────┤
│ [Nombre del producto *]                          │
│ [Slug *]              [SKU]                      │
│ [Descripción]                                    │
│ [Precio * ₲]          [Stock *]                  │
│                                                  │
│                       [Cancelar] [Guardar]        │
└──────────────────────────────────────────────────┘
```
