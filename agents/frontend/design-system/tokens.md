# Design System — Tokens

> Variables CSS y SCSS del design system. Fuente de verdad para colores, tipografía, spacing y más.

## Regla fundamental

```scss
// ✅ Siempre usar tokens
color: var(--color-primary);
margin: var(--space-4);

// ❌ Nunca hardcodear
color: #007bff;
margin: 16px;
```

---

## Colores

### Base (compartido Admin + Storefront)

```css
:root {
  /* Grises (UI base) */
  --color-gray-50:  #f9fafb;
  --color-gray-100: #f3f4f6;
  --color-gray-200: #e5e7eb;
  --color-gray-300: #d1d5db;
  --color-gray-400: #9ca3af;
  --color-gray-500: #6b7280;
  --color-gray-600: #4b5563;
  --color-gray-700: #374151;
  --color-gray-800: #1f2937;
  --color-gray-900: #111827;

  /* Semánticos */
  --color-success:  #16a34a;
  --color-warning:  #d97706;
  --color-error:    #dc2626;
  --color-info:     #2563eb;

  /* Superficie */
  --color-surface:  #ffffff;
  --color-border:   var(--color-gray-200);

  /* Texto */
  --color-text-primary:   var(--color-gray-900);
  --color-text-secondary: var(--color-gray-500);
  --color-text-disabled:  var(--color-gray-400);
  --color-text-on-primary: #ffffff;
}
```

### Personalizables por tenant (solo Storefront)

```css
:root {
  /* Valores default — sobreescritos por Store.primaryColor y Store.backgroundColor */
  --color-primary:      #0ea5e9;   /* ← Store.primaryColor */
  --color-primary-dark: #0284c7;
  --color-background:   #ffffff;   /* ← Store.backgroundColor */
}
```

```typescript
// store.service.ts — inyección del tema
applyTheme(store: StorePublicDto): void {
  const root = document.documentElement;
  if (store.primaryColor) {
    root.style.setProperty('--color-primary', store.primaryColor);
    // Calcular variante oscura (ejemplo simple)
    root.style.setProperty('--color-primary-dark', this.darken(store.primaryColor, 0.15));
  }
  if (store.backgroundColor) {
    root.style.setProperty('--color-background', store.backgroundColor);
  }
}
```

### Admin (fijo, no personalizable)

```css
:root {
  --color-primary:      #6366f1;   /* Indigo — eShopy brand */
  --color-primary-dark: #4f46e5;
  --color-sidebar-bg:   #1e1b4b;
  --color-sidebar-text: #e0e7ff;
}
```

---

## Tipografía

```css
:root {
  --font-family-base: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  --font-family-mono: 'JetBrains Mono', 'Fira Code', monospace;

  /* Escala tipográfica */
  --font-size-xs:   0.75rem;    /* 12px */
  --font-size-sm:   0.875rem;   /* 14px */
  --font-size-base: 1rem;       /* 16px */
  --font-size-lg:   1.125rem;   /* 18px */
  --font-size-xl:   1.25rem;    /* 20px */
  --font-size-2xl:  1.5rem;     /* 24px */
  --font-size-3xl:  1.875rem;   /* 30px */
  --font-size-4xl:  2.25rem;    /* 36px */

  /* Pesos */
  --font-weight-regular: 400;
  --font-weight-medium:  500;
  --font-weight-semibold: 600;
  --font-weight-bold:    700;

  /* Interlineado */
  --line-height-tight:  1.25;
  --line-height-base:   1.5;
  --line-height-relaxed: 1.75;
}
```

---

## Spacing (escala base 4px)

```css
:root {
  --space-1:  0.25rem;   /* 4px  */
  --space-2:  0.5rem;    /* 8px  */
  --space-3:  0.75rem;   /* 12px */
  --space-4:  1rem;      /* 16px */
  --space-5:  1.25rem;   /* 20px */
  --space-6:  1.5rem;    /* 24px */
  --space-8:  2rem;      /* 32px */
  --space-10: 2.5rem;    /* 40px */
  --space-12: 3rem;      /* 48px */
  --space-16: 4rem;      /* 64px */
  --space-20: 5rem;      /* 80px */
  --space-24: 6rem;      /* 96px */
}
```

---

## Border Radius

```css
:root {
  --radius-sm:   0.25rem;   /* 4px  — inputs, chips */
  --radius-base: 0.375rem;  /* 6px  — cards, botones */
  --radius-md:   0.5rem;    /* 8px  — paneles */
  --radius-lg:   0.75rem;   /* 12px — modales */
  --radius-xl:   1rem;      /* 16px — contenedores grandes */
  --radius-full: 9999px;    /* pill — badges, avatares */
}
```

---

## Sombras

```css
:root {
  --shadow-xs:  0 1px 2px 0 rgb(0 0 0 / 0.05);
  --shadow-sm:  0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1);
  --shadow-md:  0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
  --shadow-lg:  0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1);
  --shadow-xl:  0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1);
}
```

---

## Breakpoints (SCSS)

```scss
// _breakpoints.scss
$bp-xs: 600px;    // < 600px  → mobile
$bp-sm: 960px;    // 600–960px → tablet
$bp-md: 1280px;   // 960–1280px → desktop pequeño
$bp-lg: 1280px;   // > 1280px → desktop amplio

// Mixins
@mixin mobile   { @media (max-width: #{$bp-xs - 1px}) { @content; } }
@mixin tablet   { @media (min-width: #{$bp-xs}) and (max-width: #{$bp-sm - 1px}) { @content; } }
@mixin desktop  { @media (min-width: #{$bp-sm}) { @content; } }
@mixin wide     { @media (min-width: #{$bp-md}) { @content; } }
```

---

## Transiciones

```css
:root {
  --transition-fast:   150ms ease;
  --transition-base:   200ms ease;
  --transition-slow:   300ms ease;
  --transition-colors: color var(--transition-base),
                       background-color var(--transition-base),
                       border-color var(--transition-base);
}
```

---

## Z-Index

```css
:root {
  --z-dropdown:  100;
  --z-sticky:    200;
  --z-overlay:   300;
  --z-modal:     400;
  --z-toast:     500;
}
```

---

## Archivo de configuración

```
libs/ui/src/styles/
  _tokens.css          ← todas las variables :root (este archivo)
  _breakpoints.scss    ← variables y mixins de breakpoints
  _reset.scss          ← reset CSS base
  index.scss           ← barrel: @use todos los parciales
```

> Importar `libs/ui/src/styles/index.scss` en el `styles.scss` de cada app.
