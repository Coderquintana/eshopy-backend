# App — Admin Panel

> Panel de gestión para Tenant Owner/Admin/Staff. Angular 18+ standalone. Desktop-first.

## Datos de la app

| Dato | Valor |
|---|---|
| URL dev | `http://localhost:4200` |
| Directorio | `apps/admin/` |
| Auth | Keycloak OIDC (Authorization Code + PKCE) |
| Cliente Keycloak | `eshopy-admin` |
| API base | `http://localhost:5000` (configurable por entorno) |
| Audiencia | Tenant Owner, Admin, Staff |

---

## Estructura de archivos

```
apps/admin/src/app/
  app.config.ts           ← providers: HttpClient, Router, Keycloak
  app.routes.ts           ← rutas raíz con lazy loading
  core/
    services/
      auth.service.ts     ← token, permisos, user info
      api.service.ts      ← HttpClient wrapper tipado
    interceptors/
      auth.interceptor.ts    ← agrega Bearer token
      error.interceptor.ts   ← transforma HttpError → AppError
      correlation.interceptor.ts
    guards/
      auth.guard.ts          ← redirige a Keycloak si no autenticado
      permission.guard.ts    ← bloquea por permiso
    models/
      auth.models.ts         ← UserInfo, TokenPayload
  features/
    catalog/
      catalog.routes.ts
      product-list/
      product-form/
      product-status/
    orders/
      orders.routes.ts
      order-list/
      order-detail/
    store-settings/
      store-settings.routes.ts
      store-form/
    dashboard/
      dashboard.component.ts
```

---

## Rutas (app.routes.ts)

```typescript
export const appRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    component: AdminLayoutComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component')
          .then(m => m.DashboardComponent)
      },
      {
        path: 'products',
        canActivate: [permissionGuard('catalog.write')],
        loadChildren: () => import('./features/catalog/catalog.routes')
          .then(m => m.catalogRoutes)
      },
      {
        path: 'orders',
        canActivate: [permissionGuard('orders.read')],
        loadChildren: () => import('./features/orders/orders.routes')
          .then(m => m.ordersRoutes)
      },
      {
        path: 'store/settings',
        canActivate: [permissionGuard('store.write')],
        loadChildren: () => import('./features/store-settings/store-settings.routes')
          .then(m => m.storeSettingsRoutes)
      },
      {
        path: 'users',
        canActivate: [permissionGuard('users.manage')],
        loadComponent: () => import('./features/users/user-list.component')
          .then(m => m.UserListComponent)
      }
    ]
  },
  { path: 'login', component: LoginRedirectComponent }, // redirige a Keycloak
  { path: '**', redirectTo: 'dashboard' }
];
```

### Rutas del Catalog feature

```typescript
// features/catalog/catalog.routes.ts
export const catalogRoutes: Routes = [
  { path: '', loadComponent: () => import('./product-list/product-list.component').then(m => m.ProductListComponent) },
  { path: 'new', loadComponent: () => import('./product-form/product-form.component').then(m => m.ProductFormComponent) },
  { path: ':id', loadComponent: () => import('./product-form/product-form.component').then(m => m.ProductFormComponent) },
];
```

---

## Auth — Keycloak

```typescript
// core/services/auth.service.ts
@Injectable({ providedIn: 'root' })
export class AuthService {
  private keycloak = inject(KeycloakService); // keycloak-angular

  isLoggedIn = signal(false);
  userInfo   = signal<UserInfo | null>(null);
  permissions = computed(() =>
    (this.userInfo()?.['permissions'] as string[]) ?? []
  );

  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  getToken(): string {
    return this.keycloak.getToken();
  }

  login(): void { this.keycloak.login(); }
  logout(): void { this.keycloak.logout(); }
}
```

```typescript
// core/interceptors/auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }
  return next(req);
};
```

```typescript
// core/guards/auth.guard.ts
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (!auth.isLoggedIn()) {
    auth.login();
    return false;
  }
  return true;
};

// core/guards/permission.guard.ts
export const permissionGuard = (permission: string): CanActivateFn => () => {
  const auth = inject(AuthService);
  if (!auth.hasPermission(permission)) {
    inject(Router).navigate(['/403']);
    return false;
  }
  return true;
};
```

---

## Features por módulo

### Catalog (Products)

| Pantalla | Ruta | Endpoint | Permiso |
|---|---|---|---|
| Lista de productos | `/products` | `GET /api/products` | `catalog.write` |
| Crear producto | `/products/new` | `POST /api/products` | `catalog.write` |
| Editar producto | `/products/:id` | `GET + PUT /api/products/{id}` | `catalog.write` |
| Cambiar estado | Modal en lista | `PATCH /api/products/{id}/status` | `catalog.write` |

### Orders

| Pantalla | Ruta | Endpoint | Permiso |
|---|---|---|---|
| Lista de pedidos | `/orders` | `GET /api/orders` | `orders.read` |
| Detalle de pedido | `/orders/:id` | `GET /api/orders/{id}` | `orders.read` |

### Store Settings

| Pantalla | Ruta | Endpoint | Permiso |
|---|---|---|---|
| Config de tienda | `/store/settings` | `GET/PUT /api/store` | `store.write` |

---

## Configuración por ambiente

```typescript
// apps/admin/src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  keycloak: {
    url:      'http://localhost:8080',
    realm:    'eshopy',
    clientId: 'eshopy-admin'
  }
};

// apps/admin/src/environments/environment.production.ts
export const environment = {
  production: true,
  apiUrl: 'https://api.eshopy.com.py',
  keycloak: {
    url:      'https://auth.eshopy.com.py',
    realm:    'eshopy',
    clientId: 'eshopy-admin'
  }
};
```

---

## app.config.ts

```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(appRoutes, withRouterConfig({ onSameUrlNavigation: 'reload' })),
    provideHttpClient(
      withInterceptors([authInterceptor, errorInterceptor, correlationInterceptor])
    ),
    provideKeycloak({
      config: {
        url:      environment.keycloak.url,
        realm:    environment.keycloak.realm,
        clientId: environment.keycloak.clientId,
      },
      initOptions: {
        onLoad: 'check-sso',
        silentCheckSsoRedirectUri: `${window.location.origin}/assets/silent-check-sso.html`,
        pkceMethod: 'S256',
      }
    })
  ]
};
```
