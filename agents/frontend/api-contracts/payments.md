# API Contracts — Payments (Frontend)

> El frontend NO maneja webhooks. Solo inicia el pago y reacciona al retorno del provider.
> Backend: Fase 8 (no implementado). Diseño para implementación futura.

## Estado: ❌ Backend no implementado (Fase 8)

---

## Rol del frontend en el flujo de pagos

```
Frontend                   Backend                    Provider (Bancard/PagoPar)
   │                          │                                │
   │ POST /api/checkout        │                                │
   │──────────────────────────>│                                │
   │                          │── Inicia pago ─────────────────>│
   │                          │<── paymentUrl ─────────────────│
   │<── { paymentUrl } ───────│                                │
   │                          │                                │
   │ window.location.href = paymentUrl                         │
   │──────────────────────────────────────────────────────────>│
   │                          │                                │ Buyer paga
   │<────── redirect a /checkout/success?orderId=XXX ──────────│
   │                          │                                │
   │                          │<── Webhook (backend recibe) ───│
   │                          │    Payment → Captured          │
   │                          │    Order   → Paid              │
   │                          │                                │
   │ GET /api/orders/{id}      │ (polling para verificar)       │
   │──────────────────────────>│                                │
   │<── OrderDto { status: 1 }│                                │
```

**El frontend NO:**
- Llama a endpoints de webhook
- Valida firmas de pago
- Calcula montos de pago
- Hace integración directa con Bancard/PagoPar

**El frontend SÍ:**
- Redirige al buyer a `paymentUrl` (recibida del backend)
- Recibe al buyer de vuelta en `/checkout/success` o `/checkout/failed`
- Hace polling a `GET /api/orders/{id}` para verificar el estado del pago

---

## Interfaces TypeScript

```typescript
// models/payment.models.ts

// El frontend recibe paymentUrl dentro de la respuesta del checkout
// No hay un DTO de Payment separado que el frontend necesite conocer

// Respuesta del checkout (ya definido en orders.md)
export interface CheckoutResponse {
  orderId: string;
  orderNumber: number;
  totalAmount: number;
  currencyCode: string;
  paymentUrl: string;    // ← URL de Bancard/PagoPar
}

// Query params que el provider envía al volver
export interface PaymentReturnParams {
  orderId: string;       // enviado por el backend como returnUrl param
  // El provider puede agregar sus propios params (ignorarlos en el frontend)
}
```

---

## Flujo de redirección al provider

```typescript
// checkout-form.component.ts

onSubmit() {
  if (this.form.invalid) return;
  this.submitting.set(true);

  const request: CheckoutRequest = {
    cartToken: this.cartService.getToken(),
    buyerEmail: this.form.value.buyerEmail,
    buyerName:  this.form.value.buyerName,
    shippingAddress: this.form.value.shippingAddress,
  };

  this.orderService.checkout(request)
    .pipe(finalize(() => this.submitting.set(false)))
    .subscribe({
      next: (response) => {
        // Limpiar carrito local (el backend ya procesó el checkout)
        localStorage.removeItem('eshopy_cart_token');

        // Redirigir al proveedor de pago — salir de la SPA
        window.location.href = response.paymentUrl;
      },
      error: (err) => this.handleCheckoutError(err)
    });
}
```

---

## URLs de retorno (configurar en el backend)

El backend configura estas URLs como `returnUrl` al llamar al provider:

| Resultado | URL de retorno |
|---|---|
| Pago exitoso | `https://{subdomain}.eshopy.com.py/checkout/success?orderId={orderId}` |
| Pago fallido | `https://{subdomain}.eshopy.com.py/checkout/failed?orderId={orderId}` |
| Cancelado    | `https://{subdomain}.eshopy.com.py/checkout/failed?orderId={orderId}` |

> El frontend NO configura estas URLs — el backend las define al llamar al provider.

---

## Polling para verificar estado del pago

```typescript
// core/services/order.service.ts

async waitForPaymentConfirmation(
  orderId: string,
  maxRetries = 10,
  intervalMs = 2000
): Promise<OrderDto> {

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    const order = await firstValueFrom(
      this.http.get<OrderDto>(`/api/orders/${orderId}`)
    );

    if (order.status !== 0) {
      // Ya no está PendingPayment — el webhook del backend actualizó el estado
      return order;
    }

    if (attempt < maxRetries - 1) {
      await new Promise(resolve => setTimeout(resolve, intervalMs));
    }
  }

  // Timeout: retornar el último estado conocido
  return await firstValueFrom(this.http.get<OrderDto>(`/api/orders/${orderId}`));
}
```

---

## PaymentReturnComponent — lógica completa

```typescript
// features/checkout/payment-return/payment-return.component.ts

@Component({
  standalone: true,
  imports: [AppButton, AppLoading, CurrencyPipe, RouterLink],
  template: `
    <div class="payment-return">
      @if (loading()) {
        <app-loading size="lg" />
        <p>Verificando tu pago... por favor esperá.</p>
      } @else if (order()?.status === 1) {
        <!-- ✅ Pagado -->
        <div class="success">
          <h1>¡Gracias por tu compra!</h1>
          <p class="order-number">Pedido N° <strong>{{ order()?.orderNumber }}</strong></p>
          <p>Te enviamos la confirmación a <strong>{{ order()?.buyerEmail }}</strong>.</p>
          <p class="total">Total pagado: <strong>{{ formatPYG(order()?.totalAmount ?? 0) }}</strong></p>
          <app-button routerLink="/products">Seguir comprando</app-button>
        </div>
      } @else if (order()?.status === 2) {
        <!-- ❌ Cancelado / Fallido -->
        <div class="failed">
          <h1>El pago no se completó</h1>
          <p>Tu pedido N° {{ order()?.orderNumber }} fue cancelado.</p>
          <p>No se realizó ningún cobro.</p>
          <app-button routerLink="/cart">Volver al carrito</app-button>
        </div>
      } @else {
        <!-- ⏳ Timeout o estado desconocido -->
        <div class="pending">
          <h1>Verificando tu pago...</h1>
          <p>Estamos confirmando tu transacción. Esto puede demorar unos minutos.</p>
          <p>Si ya pagaste y no recibís confirmación, contactá a la tienda.</p>
          <app-button (click)="retry()">Verificar nuevamente</app-button>
        </div>
      }
    </div>
  `
})
export class PaymentReturnComponent implements OnInit {
  private orderService = inject(OrderService);
  private route        = inject(ActivatedRoute);

  loading = signal(true);
  order   = signal<OrderDto | null>(null);

  async ngOnInit() {
    const orderId = this.route.snapshot.queryParams['orderId'];
    if (orderId) {
      try {
        const order = await this.orderService.waitForPaymentConfirmation(orderId);
        this.order.set(order);
      } catch {
        // Error de red — mostrar estado pendiente
      }
    }
    this.loading.set(false);
  }

  async retry() {
    this.loading.set(true);
    await this.ngOnInit();
  }

  formatPYG(amount: number): string {
    return new Intl.NumberFormat('es-PY', {
      style: 'currency', currency: 'PYG',
      minimumFractionDigits: 0
    }).format(amount);
  }
}
```

---

## Errores posibles

| Código | Cuándo ocurre | Acción en UI |
|---|---|---|
| `PAYMENT_PROVIDER_ERROR` | Backend no pudo contactar a Bancard/PagoPar | "Error al iniciar el pago. Intentá en unos minutos." |
| `NOT_FOUND` | orderId inválido en el retorno | Mostrar "No encontramos tu pedido" |
| `GENERIC_ERROR` | Error inesperado en backend | "Ocurrió un error. Contactá a la tienda." |
