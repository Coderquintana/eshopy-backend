# Workflow — Checkout Flow (Frontend)

> Flujo completo del buyer: catálogo → carrito → formulario → pago → confirmación.

---

## Pantallas del flujo

```
/products        /products/:slug     /cart           /checkout      [provider]     /checkout/success
     │                 │               │                │                │               │
  [Grid]          [Detalle]       [Lista items]    [Formulario]    [Pagar en       [Confirmación]
  [Agregar]       [Agregar]       [Totales]        [Datos buyer]    Bancard]
                                  [Ir al checkout] [Ir al pago]
```

---

## Paso 1: Catálogo y detalle de producto

```typescript
// features/catalog/product-list/product-list.component.ts
@Component({
  template: `
    <div class="product-grid">
      @for (product of products()?.items; track product.id) {
        <div class="product-card">
          <div class="product-image">
            <!-- Placeholder hasta que ProductImages esté implementado -->
            <div class="image-placeholder"></div>
          </div>
          <h3>{{ product.name }}</h3>
          <p class="price">{{ formatPYG(product.price) }}</p>
          <app-button (click)="addToCart(product.id)">Agregar al carrito</app-button>
        </div>
      }
    </div>

    <!-- Paginación -->
    @if ((products()?.totalPages ?? 0) > 1) {
      <div class="pagination">
        @for (p of pages(); track p) {
          <app-button [variant]="p === page() ? 'primary' : 'ghost'" (click)="goToPage(p)">
            {{ p }}
          </app-button>
        }
      </div>
    }
  `
})
export class ProductListComponent implements OnInit {
  private http    = inject(HttpClient);
  private cart    = inject(CartService);
  private toast   = inject(ToastService);

  page     = signal(1);
  products = signal<PagedResult<ProductPublicDto> | null>(null);
  loading  = signal(false);
  pages    = computed(() =>
    Array.from({ length: this.products()?.totalPages ?? 0 }, (_, i) => i + 1)
  );

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.http.get<PagedResult<ProductPublicDto>>('/api/public/products', {
      params: { page: this.page(), pageSize: 20 }
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({ next: r => this.products.set(r) });
  }

  addToCart(productId: string) {
    this.cart.addItem(productId).subscribe({
      next: () => this.toast.success('Producto agregado al carrito.'),
      error: err => {
        if (err.error?.code === 'PRODUCT_NOT_AVAILABLE')
          this.toast.error('Este producto ya no está disponible.');
        else
          this.toast.error('No pudimos agregar el producto.');
      }
    });
  }

  goToPage(p: number) { this.page.set(p); this.load(); }
}
```

---

## Paso 2: Carrito

```typescript
// features/cart/cart.component.ts
@Component({
  template: `
    <h1>Tu carrito</h1>

    @if (cart()?.items?.length === 0 || !cart()) {
      <p>Tu carrito está vacío.</p>
      <app-button routerLink="/products">Ver productos</app-button>
    } @else {
      @for (item of cart()!.items; track item.id) {
        <div class="cart-item">
          <span class="item-name">{{ item.productName }}</span>
          <div class="item-quantity">
            <app-button variant="ghost" size="sm"
              (click)="updateQuantity(item.id, item.quantity - 1)"
              [disabled]="item.quantity <= 1">−</app-button>
            <span>{{ item.quantity }}</span>
            <app-button variant="ghost" size="sm"
              (click)="updateQuantity(item.id, item.quantity + 1)">+</app-button>
          </div>
          <span class="item-price">{{ formatPYG(item.subtotal) }}</span>
          <app-button variant="ghost" size="sm" (click)="removeItem(item.id)">✕</app-button>
        </div>
      }

      <div class="cart-summary">
        <!-- SIEMPRE mostrar el total del backend — NUNCA calcular en frontend -->
        <strong>Total: {{ formatPYG(cart()!.totalAmount) }}</strong>
      </div>

      <app-button routerLink="/checkout" variant="primary">
        Continuar con el pago
      </app-button>
    }
  `
})
export class CartComponent implements OnInit {
  private cartService = inject(CartService);
  private toast       = inject(ToastService);

  cart = this.cartService.cart;

  ngOnInit() {
    this.cartService.loadCart().subscribe();
  }

  updateQuantity(itemId: string, quantity: number) {
    if (quantity < 1) return;
    this.cartService.updateItem(itemId, quantity).subscribe({
      error: () => this.toast.error('No pudimos actualizar la cantidad.')
    });
  }

  removeItem(itemId: string) {
    this.cartService.removeItem(itemId).subscribe({
      next: () => this.toast.info('Producto eliminado del carrito.'),
      error: () => this.toast.error('No pudimos eliminar el producto.')
    });
  }
}
```

---

## Paso 3: Formulario de checkout

```typescript
// features/checkout/checkout-form/checkout-form.component.ts
@Component({
  template: `
    <div class="checkout-layout">
      <!-- Formulario con datos del comprador -->
      <section class="checkout-form">
        <h2>Datos para tu pedido</h2>
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <app-text-field label="Nombre completo *" formControlName="buyerName" />
          <app-text-field label="Email *" formControlName="buyerEmail" type="email"
            hint="Te enviaremos la confirmación a este email." />
          <app-text-field label="Dirección de envío" formControlName="shippingAddress" />

          <app-button type="submit" [disabled]="form.invalid || submitting()"
            [loading]="submitting()" fullWidth>
            Ir al pago
          </app-button>
        </form>
      </section>

      <!-- Resumen del carrito (readonly) -->
      <aside class="checkout-summary">
        <h2>Resumen de tu pedido</h2>
        @for (item of cart()?.items; track item.id) {
          <div class="summary-item">
            <span>{{ item.productName }} x{{ item.quantity }}</span>
            <span>{{ formatPYG(item.subtotal) }}</span>
          </div>
        }
        <hr />
        <div class="summary-total">
          <strong>Total</strong>
          <!-- Del backend — nunca calcular -->
          <strong>{{ formatPYG(cart()?.totalAmount ?? 0) }}</strong>
        </div>
      </aside>
    </div>
  `
})
export class CheckoutFormComponent {
  private fb          = inject(FormBuilder);
  private orderService = inject(OrderService);
  private cartService  = inject(CartService);
  private toast        = inject(ToastService);

  cart       = this.cartService.cart;
  submitting = signal(false);

  form = this.fb.group({
    buyerName:       ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    buyerEmail:      ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    shippingAddress: ['', [Validators.maxLength(500)]],
  });

  onSubmit() {
    if (this.form.invalid || !this.cart()) return;
    this.submitting.set(true);

    const request: CheckoutRequest = {
      ...this.form.value as Omit<CheckoutRequest, 'cartToken'>,
      cartToken: this.cartService.getToken(),
    };

    this.orderService.checkout(request)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: response => {
          // Limpiar token de carrito — el carrito fue procesado
          localStorage.removeItem('eshopy_cart_token');
          // Redirigir al proveedor de pago (salir de la SPA)
          window.location.href = response.paymentUrl;
        },
        error: err => this.handleError(err)
      });
  }

  private handleError(err: HttpErrorResponse) {
    const code = err.error?.code;
    if (code === 'PRODUCT_NOT_AVAILABLE') {
      this.toast.error('Algunos productos ya no están disponibles. Revisá tu carrito.');
      // No navegar — dejar que el usuario vea el error y vuelva al carrito
    } else if (code === 'VALIDATION_ERROR') {
      this.toast.error('Revisá los datos ingresados.');
    } else {
      this.toast.error('No pudimos procesar tu pedido. Intentá más tarde.');
    }
  }
}
```

---

## Paso 4: Pago en el provider (fuera de la SPA)

El buyer paga en el sitio de Bancard o PagoPar. El frontend no tiene control aquí.

---

## Paso 5: Retorno y confirmación

Ver `api-contracts/payments.md` para el `PaymentReturnComponent` completo.

---

## Resumen de reglas de negocio (para el frontend)

| Regla | Implementación frontend |
|---|---|
| No calcular totales | Mostrar `cart.totalAmount` y `order.totalAmount` del backend |
| No validar stock | Mostrar el error `PRODUCT_NOT_AVAILABLE` del backend con mensaje amigable |
| No validar si el carrito es válido | El backend lo valida en `/api/checkout` |
| CartToken en header, no en body | `{ headers: { 'X-Cart-Token': token } }` |
| Limpiar CartToken al hacer checkout | `localStorage.removeItem('eshopy_cart_token')` antes de redirigir |
| Polling para confirmar pago | Máximo 10 reintentos × 2 segundos = 20 segundos de espera |
