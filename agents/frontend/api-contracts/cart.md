# API Contracts — Cart (Frontend)

> Interfaces TypeScript y manejo de errores para el carrito de compras.
> Backend: Fase 6 (no implementado). Diseño para implementación futura.

## Estado: ❌ Backend no implementado (Fase 6)

---

## Interfaces TypeScript

```typescript
// models/cart.models.ts

export interface CartItemDto {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  productImageUrl: string | null;
  unitPrice: number;
  currencyCode: string;
  quantity: number;
  subtotal: number;   // calculado por el backend (unitPrice * quantity)
}

export interface CartDto {
  cartToken: string;
  items: CartItemDto[];
  itemCount: number;
  totalAmount: number;  // calculado por el backend — NUNCA recalcular en frontend
  currencyCode: string;
  expiresAtUtc: string; // ISO 8601
}

export interface AddCartItemRequest {
  productId: string;
  quantity: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}
```

---

## CartToken — gestión en frontend

```typescript
// El CartToken es un UUID generado en el frontend y persistido en localStorage
// Se envía en el header X-Cart-Token en CADA request al backend de carrito
// El backend crea el carrito si el token no existe aún

const CART_TOKEN_KEY = 'eshopy_cart_token';

function getOrCreateCartToken(): string {
  let token = localStorage.getItem(CART_TOKEN_KEY);
  if (!token) {
    token = crypto.randomUUID();
    localStorage.setItem(CART_TOKEN_KEY, token);
  }
  return token;
}
```

---

## Endpoints y llamadas

### GET /api/cart (obtener carrito)

```typescript
// Cabecera obligatoria: X-Cart-Token
getCart(): Observable<CartDto> {
  return this.http.get<CartDto>('/api/cart', {
    headers: { 'X-Cart-Token': this.getCartToken() }
  });
}
```

### POST /api/cart/items (agregar item)

```typescript
addItem(productId: string, quantity = 1): Observable<CartDto> {
  return this.http.post<CartDto>(
    '/api/cart/items',
    { productId, quantity } satisfies AddCartItemRequest,
    { headers: { 'X-Cart-Token': this.getCartToken() } }
  );
}

// Uso en ProductDetail
onAddToCart() {
  this.cartService.addItem(this.product().id, this.quantity())
    .subscribe({
      next: () => this.toast.success('Producto agregado al carrito.'),
      error: err => this.handleCartError(err)
    });
}
```

### PUT /api/cart/items/{id} (actualizar cantidad)

```typescript
updateItem(itemId: string, quantity: number): Observable<CartDto> {
  return this.http.put<CartDto>(
    `/api/cart/items/${itemId}`,
    { quantity } satisfies UpdateCartItemRequest,
    { headers: { 'X-Cart-Token': this.getCartToken() } }
  );
}
```

### DELETE /api/cart/items/{id} (eliminar item)

```typescript
removeItem(itemId: string): Observable<CartDto> {
  return this.http.delete<CartDto>(
    `/api/cart/items/${itemId}`,
    { headers: { 'X-Cart-Token': this.getCartToken() } }
  );
}
```

---

## Validaciones UX

```typescript
// ✅ Validar en frontend (UX inmediata)
quantity: [1, [Validators.required, Validators.min(1), Validators.max(99)]]

// ❌ NUNCA validar en frontend
checkStockAvailability()    // el backend controla el stock
calculateSubtotal()         // backend calcula, frontend solo muestra
validateCartBeforeCheckout() // el backend valida en /api/checkout
```

---

## Manejo de errores

```typescript
private handleCartError(err: HttpErrorResponse): void {
  const error = err.error as ErrorResponse;

  switch (error?.code) {
    case 'PRODUCT_NOT_AVAILABLE':
      // Producto archivado o eliminado desde que se cargó la página
      this.toast.error('Este producto ya no está disponible.');
      // Recargar la lista de productos para mostrar estado actualizado
      this.loadProducts();
      break;

    case 'NOT_FOUND':
      this.toast.error('El carrito no fue encontrado. Recargá la página.');
      break;

    case 'VALIDATION_ERROR':
      this.toast.error('Cantidad inválida.');
      break;

    default:
      this.toast.error('No pudimos actualizar el carrito. Intentá más tarde.');
  }
}
```

| Código | Cuándo ocurre | Acción en UI |
|---|---|---|
| `PRODUCT_NOT_AVAILABLE` | Producto archivado/no activo | "Producto no disponible". Recargar lista |
| `NOT_FOUND` | CartToken inválido | Generar nuevo token + mensaje |
| `VALIDATION_ERROR` | Cantidad fuera de rango | Toast error |
| `TENANT_NOT_FOUND` | Subdominio inválido | Pantalla de error global |

---

## CartComponent — estructura visual

```typescript
@Component({
  template: `
    @if (cart().items.length === 0) {
      <div class="empty-cart">
        <p>Tu carrito está vacío.</p>
        <app-button routerLink="/products">Ver productos</app-button>
      </div>
    } @else {
      @for (item of cart().items; track item.id) {
        <div class="cart-item">
          <span>{{ item.productName }}</span>

          <!-- Controles de cantidad — con debounce para no spamear el API -->
          <input type="number" [value]="item.quantity"
            (change)="updateQuantity(item.id, $event)" min="1" max="99" />

          <span>{{ formatPYG(item.subtotal) }}</span>

          <app-button variant="ghost" (click)="removeItem(item.id)">✕</app-button>
        </div>
      }

      <div class="cart-total">
        <!-- El total viene del backend, NUNCA lo calcules en frontend -->
        <strong>Total: {{ formatPYG(cart().totalAmount) }}</strong>
      </div>

      <app-button routerLink="/checkout" [disabled]="cart().items.length === 0">
        Ir al checkout
      </app-button>
    }
  `
})
export class CartComponent {
  cart = inject(CartService).cart;
}
```

> **Regla crítica**: `totalAmount` y `subtotal` siempre vienen del backend. Nunca hacer `price * quantity` en el frontend.
