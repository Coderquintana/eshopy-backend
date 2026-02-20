# API Contracts — Orders (Frontend)

> Interfaces TypeScript, checkout request y manejo de errores para pedidos.
> Backend: Fase 7 (no implementado). Diseño para implementación futura.

## Estado: ❌ Backend no implementado (Fase 7)

---

## Interfaces TypeScript

```typescript
// models/order.models.ts

export type OrderStatus = 0 | 1 | 2 | 3;
// 0 = PendingPayment, 1 = Paid, 2 = Cancelled, 3 = Refunded

export interface OrderItemDto {
  id: string;
  productId: string;
  productName: string;     // snapshot al momento del checkout
  productSku: string | null;
  unitPrice: number;       // snapshot al momento del checkout
  quantity: number;
  subtotal: number;        // calculado por el backend
}

export interface OrderDto {
  id: string;
  orderNumber: number;
  status: OrderStatus;
  buyerEmail: string;
  buyerName: string;
  shippingAddress: string | null;
  items: OrderItemDto[];
  totalAmount: number;
  currencyCode: string;
  paymentUrl: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

// Request de checkout — enviado por el Storefront
export interface CheckoutRequest {
  cartToken: string;       // token del carrito actual
  buyerEmail: string;
  buyerName: string;
  shippingAddress?: string;
}

// Response del checkout
export interface CheckoutResponse {
  orderId: string;
  orderNumber: number;
  totalAmount: number;
  currencyCode: string;
  paymentUrl: string;      // URL de Bancard/PagoPar donde el buyer paga
}

// Para lista de pedidos (Admin)
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
```

---

## Validaciones UX del formulario de checkout

```typescript
// features/checkout/checkout-form/checkout-form.component.ts

this.form = this.fb.group({
  buyerName: ['', [
    Validators.required,
    Validators.minLength(2),
    Validators.maxLength(200)
  ]],
  buyerEmail: ['', [
    Validators.required,
    Validators.email,
    Validators.maxLength(200)
  ]],
  shippingAddress: ['', [
    Validators.maxLength(500)
    // No es required en MVP — depende de si el store hace delivery
  ]],
  // cartToken: se obtiene de CartService — no es un campo del formulario
});
```

**Qué NO validar en frontend:**
```typescript
// ❌ Nunca hacer esto
validateCartItems()        // el backend valida que los productos sigan activos
calculateTotal()           // backend calcula, no frontend
checkStockAvailability()   // backend controla stock
validateEmailFormat()      // Validators.email es suficiente
validateOrderStatus()      // transiciones son del backend
```

---

## Endpoints y llamadas

### POST /api/checkout (crear pedido desde carrito)

```typescript
// Enviado por el Storefront al completar el formulario de checkout
checkout(request: CheckoutRequest): Observable<CheckoutResponse> {
  return this.http.post<CheckoutResponse>('/api/checkout', request);
}

// En CheckoutFormComponent:
onSubmit() {
  if (this.form.invalid) return;
  this.submitting.set(true);

  const request: CheckoutRequest = {
    ...this.form.value,
    cartToken: this.cartService.getToken(),
  };

  this.orderService.checkout(request)
    .pipe(finalize(() => this.submitting.set(false)))
    .subscribe({
      next: response => {
        // Redirigir al proveedor de pago
        window.location.href = response.paymentUrl;
      },
      error: err => this.handleCheckoutError(err)
    });
}
```

### GET /api/orders (lista admin)

```typescript
// Solo en Admin — requiere token Bearer con orders.read
getOrders(page = 1, pageSize = 20): Observable<PagedResult<OrderDto>> {
  return this.http.get<PagedResult<OrderDto>>('/api/orders', {
    params: { page, pageSize }
  });
}
```

### GET /api/orders/{id} (detalle)

```typescript
getOrder(id: string): Observable<OrderDto> {
  return this.http.get<OrderDto>(`/api/orders/${id}`);
}

// Usado en payment-return para confirmar estado del pedido
// El frontend hace polling hasta que el backend confirme el pago
async waitForPaymentConfirmation(orderId: string): Promise<OrderStatus> {
  const maxRetries = 10;
  const delayMs = 2000;

  for (let i = 0; i < maxRetries; i++) {
    const order = await firstValueFrom(this.getOrder(orderId));
    if (order.status !== 0) return order.status; // ya no está PendingPayment
    await new Promise(r => setTimeout(r, delayMs));
  }
  return 0; // timeout — mostrar "verificando..."
}
```

---

## Manejo de errores

```typescript
private handleCheckoutError(err: HttpErrorResponse): void {
  const error = err.error as ErrorResponse;

  switch (error?.code) {
    case 'PRODUCT_NOT_AVAILABLE':
      // Uno o más productos del carrito ya no están activos
      this.toast.error(
        'Algunos productos de tu carrito ya no están disponibles. ' +
        'Revisá tu carrito antes de continuar.'
      );
      this.router.navigate(['/cart']);
      break;

    case 'VALIDATION_ERROR':
      this.applyServerValidationErrors(error.details as Record<string, string[]>);
      this.toast.error('Revisá los datos del formulario.');
      break;

    case 'NOT_FOUND':
      this.toast.error('No encontramos tu carrito. Intentá agregar productos nuevamente.');
      this.router.navigate(['/products']);
      break;

    default:
      this.toast.error('No pudimos procesar tu pedido. Intentá más tarde.');
  }
}
```

| Código | Cuándo ocurre | Acción en UI |
|---|---|---|
| `PRODUCT_NOT_AVAILABLE` | Producto archivado desde que se agregó al carrito | Volver al carrito con aviso |
| `VALIDATION_ERROR` | Datos del comprador inválidos | Errores por campo |
| `NOT_FOUND` | CartToken sin carrito | Volver a productos |
| `PAYMENT_PROVIDER_ERROR` | Error al contactar Bancard/PagoPar | "Error al iniciar pago, reintentá" |

---

## Mapeo OrderStatus → UI

```typescript
export const ORDER_STATUS_LABEL: Record<OrderStatus, string> = {
  0: 'Pendiente de pago',
  1: 'Pagado',
  2: 'Cancelado',
  3: 'Reembolsado',
};

export const ORDER_STATUS_BADGE: Record<OrderStatus, 'warning' | 'success' | 'error' | 'info'> = {
  0: 'warning',   // Pendiente → amarillo
  1: 'success',   // Pagado    → verde
  2: 'error',     // Cancelado → rojo
  3: 'info',      // Reembolsado → azul
};
```

---

## PaymentReturnComponent — pantalla post-pago

```typescript
// /checkout/success?orderId=xxx  o  /checkout/failed?orderId=xxx

@Component({
  template: `
    @if (loading()) {
      <app-loading size="lg" />
      <p>Verificando el estado de tu pago...</p>
    } @else if (order()?.status === 1) {
      <!-- Paid -->
      <h1>¡Pedido confirmado!</h1>
      <p>Pedido N° {{ order()?.orderNumber }}</p>
      <p>Total: {{ formatPYG(order()?.totalAmount ?? 0) }}</p>
      <app-button routerLink="/products">Seguir comprando</app-button>
    } @else {
      <!-- Cancelled / Failed / Unknown -->
      <h1>El pago no se pudo completar</h1>
      <p>Podés intentar nuevamente o contactar al soporte.</p>
      <app-button routerLink="/cart">Volver al carrito</app-button>
    }
  `
})
export class PaymentReturnComponent implements OnInit {
  private orderService = inject(OrderService);
  private route = inject(ActivatedRoute);

  loading = signal(true);
  order   = signal<OrderDto | null>(null);

  async ngOnInit() {
    const orderId = this.route.snapshot.queryParams['orderId'];
    if (orderId) {
      const status = await this.orderService.waitForPaymentConfirmation(orderId);
      const order  = await firstValueFrom(this.orderService.getOrder(orderId));
      this.order.set(order);
    }
    this.loading.set(false);
  }
}
```
