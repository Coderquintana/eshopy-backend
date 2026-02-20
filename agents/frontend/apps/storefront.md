# App — Storefront

> Tienda pública del tenant. Angular 18+ standalone. Mobile-first. Buyers anónimos en MVP.

## Datos de la app

| Dato | Valor |
|---|---|
| URL dev | `http://localhost:4201` |
| Directorio | `apps/storefront/` |
| Auth | Anónimo en MVP. Sin login de buyer. |
| API base | `http://localhost:5000` |
| Audiencia | Buyers (público general) |
| Dominio prod | `{subdomain}.eshopy.com.py` |

---

## Estructura de archivos

```
apps/storefront/src/app/
  app.config.ts              ← providers: HttpClient, Router
  app.routes.ts              ← rutas raíz
  app.component.ts           ← AppHeader + RouterOutlet + AppFooter
  core/
    services/
      store.service.ts       ← carga Store info + aplica tema
      cart.service.ts        ← CartToken + estado del carrito (signals)
      api.service.ts         ← HttpClient wrapper tipado
    interceptors/
      error.interceptor.ts
      correlation.interceptor.ts
    models/
      store.models.ts        ← StorePublicDto
      cart.models.ts         ← CartDto, CartItemDto
  features/
    home/
      home.component.ts
    catalog/
      catalog.routes.ts
      product-list/
        product-list.component.ts   ← grid de productos
      product-detail/
        product-detail.component.ts ← detalle por slug
    cart/
      cart.component.ts
    checkout/
      checkout.routes.ts
      checkout-form/
        checkout-form.component.ts
      payment-return/
        payment-return.component.ts  ← /checkout/success y /checkout/failed
```

---

## Rutas (app.routes.ts)

```typescript
export const appRoutes: Routes = [
  {
    path: '',
    component: AppComponent,
    children: [
      {
        path: '',
        loadComponent: () => import('./features/home/home.component')
          .then(m => m.HomeComponent)
      },
      {
        path: 'products',
        loadChildren: () => import('./features/catalog/catalog.routes')
          .then(m => m.catalogRoutes)
      },
      {
        path: 'cart',
        loadComponent: () => import('./features/cart/cart.component')
          .then(m => m.CartComponent)
      },
      {
        path: 'checkout',
        loadChildren: () => import('./features/checkout/checkout.routes')
          .then(m => m.checkoutRoutes)
      }
    ]
  }
];

// features/catalog/catalog.routes.ts
export const catalogRoutes: Routes = [
  { path: '', loadComponent: () => import('./product-list/product-list.component').then(m => m.ProductListComponent) },
  { path: ':slug', loadComponent: () => import('./product-detail/product-detail.component').then(m => m.ProductDetailComponent) },
];

// features/checkout/checkout.routes.ts
export const checkoutRoutes: Routes = [
  { path: '', loadComponent: () => import('./checkout-form/checkout-form.component').then(m => m.CheckoutFormComponent) },
  { path: 'success', loadComponent: () => import('./payment-return/payment-return.component').then(m => m.PaymentReturnComponent) },
  { path: 'failed',  loadComponent: () => import('./payment-return/payment-return.component').then(m => m.PaymentReturnComponent) },
];
```

---

## StoreService — Inicialización del tema

```typescript
// core/services/store.service.ts
@Injectable({ providedIn: 'root' })
export class StoreService {
  private http = inject(HttpClient);

  store = signal<StorePublicDto | null>(null);
  storeName    = computed(() => this.store()?.name ?? 'eShopy');
  currencyCode = computed(() => this.store()?.currencyCode ?? 'PYG');

  async initialize(): Promise<void> {
    const store = await firstValueFrom(
      this.http.get<StorePublicDto>('/api/store')
    );
    this.store.set(store);
    this.applyTheme(store);
  }

  private applyTheme(store: StorePublicDto): void {
    const root = document.documentElement;
    if (store.primaryColor)    root.style.setProperty('--color-primary', store.primaryColor);
    if (store.backgroundColor) root.style.setProperty('--color-background', store.backgroundColor);
  }
}

// app.config.ts — inicializar antes de renderizar
{
  provide: APP_INITIALIZER,
  useFactory: (store: StoreService) => () => store.initialize(),
  deps: [StoreService],
  multi: true
}
```

---

## CartService — Gestión del carrito

```typescript
// core/services/cart.service.ts
@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);
  private readonly CART_TOKEN_KEY = 'eshopy_cart_token';

  private cartToken = signal<string>(this.getOrCreateToken());
  private _cart = signal<CartDto | null>(null);

  cart      = this._cart.asReadonly();
  itemCount = computed(() => this._cart()?.items.reduce((s, i) => s + i.quantity, 0) ?? 0);
  total     = computed(() => this._cart()?.totalAmount ?? 0);

  private getOrCreateToken(): string {
    let token = localStorage.getItem(this.CART_TOKEN_KEY);
    if (!token) {
      token = crypto.randomUUID();
      localStorage.setItem(this.CART_TOKEN_KEY, token);
    }
    return token;
  }

  private get headers() {
    return { 'X-Cart-Token': this.cartToken() };
  }

  loadCart(): Observable<CartDto> {
    return this.http.get<CartDto>('/api/cart', { headers: this.headers })
      .pipe(tap(cart => this._cart.set(cart)));
  }

  addItem(productId: string, quantity = 1): Observable<CartDto> {
    return this.http.post<CartDto>('/api/cart/items',
      { productId, quantity },
      { headers: this.headers }
    ).pipe(tap(cart => this._cart.set(cart)));
  }

  updateItem(itemId: string, quantity: number): Observable<CartDto> {
    return this.http.put<CartDto>(`/api/cart/items/${itemId}`,
      { quantity },
      { headers: this.headers }
    ).pipe(tap(cart => this._cart.set(cart)));
  }

  removeItem(itemId: string): Observable<CartDto> {
    return this.http.delete<CartDto>(`/api/cart/items/${itemId}`,
      { headers: this.headers }
    ).pipe(tap(cart => this._cart.set(cart)));
  }

  getToken(): string { return this.cartToken(); }
}
```

---

## Features por pantalla

### Home `/`

| Contenido | Fuente |
|---|---|
| Banner principal (nombre + descripción del store) | `StoreService.store()` |
| Grid de productos destacados (primeros 8 activos) | `GET /api/public/products?pageSize=8` |
| Call to action: "Ver todo el catálogo" | → `/products` |

### Catálogo `/products`

| Elemento | Descripción |
|---|---|
| Grid de product cards | `GET /api/public/products?page=X&pageSize=20` |
| Paginación | Componente AppDataGrid o paginación custom |
| ProductCard | Imagen (placeholder), nombre, precio formateado, botón "Agregar al carrito" |

### Detalle de producto `/products/:slug`

| Elemento | Descripción |
|---|---|
| Nombre, descripción, precio | `GET /api/public/products/{slug}` |
| Botón "Agregar al carrito" | `CartService.addItem()` |
| Stock no se muestra | El frontend no sabe el stock — solo el backend controla disponibilidad |

### Carrito `/cart`

| Elemento | Descripción |
|---|---|
| Lista de items | `CartService.cart()` |
| Nombre, precio, cantidad, subtotal | Del CartDto |
| Totales | `CartService.total()` (calculado por backend, no en frontend) |
| Botón "Ir al checkout" | → `/checkout` |

### Checkout `/checkout`

Ver `workflows/checkout-flow.md` para el flujo completo.

### Payment Return `/checkout/success` y `/checkout/failed`

```typescript
// payment-return.component.ts
export class PaymentReturnComponent {
  private route = inject(ActivatedRoute);
  private http  = inject(HttpClient);

  isSuccess = computed(() => this.route.snapshot.url[0]?.path === 'success');
  orderId   = signal(this.route.snapshot.queryParams['orderId'] ?? '');
  order     = signal<OrderDto | null>(null);

  ngOnInit() {
    if (this.orderId()) {
      this.http.get<OrderDto>(`/api/orders/${this.orderId()}`)
        .subscribe(order => this.order.set(order));
    }
  }
}
```

---

## Formato de precios (PYG)

```typescript
// core/utils/currency.utils.ts
export function formatPYG(amount: number): string {
  return new Intl.NumberFormat('es-PY', {
    style: 'currency',
    currency: 'PYG',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
  // → "₲ 85.000"
}
```

---

## app.config.ts

```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(appRoutes),
    provideHttpClient(
      withInterceptors([errorInterceptor, correlationInterceptor])
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: (store: StoreService) => () => store.initialize(),
      deps: [StoreService],
      multi: true
    }
  ]
};
```
