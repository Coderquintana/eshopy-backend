# API Contracts — Products (Frontend)

> Interfaces TypeScript, validaciones UX y manejo de errores para el módulo Catalog.
> Sincronizado con `../../architecture/api-contracts.md`.

---

## Interfaces TypeScript

```typescript
// models/product.models.ts

export type ProductStatus = 0 | 1 | 2;  // 0=Draft, 1=Active, 2=Archived

export interface ProductAdminDto {
  id: string;
  slug: string;
  sku: string | null;
  name: string;
  description: string | null;
  price: number;
  currencyCode: string;
  status: ProductStatus;
  stockOnHand: number;
  createdAtUtc: string;   // ISO 8601
  updatedAtUtc: string | null;
}

export interface ProductPublicDto {
  id: string;
  slug: string;
  name: string;
  description: string | null;
  price: number;
  currencyCode: string;
}

export interface CreateProductRequest {
  slug: string;
  sku?: string | null;
  name: string;
  description?: string | null;
  price: number;
  stockOnHand: number;
  // currencyCode NO se envía — el backend lo toma del Store
}

export interface UpdateProductRequest {
  name: string;
  description?: string | null;
  price: number;
  stockOnHand: number;
  sku?: string | null;
}

export interface ChangeStatusRequest {
  status: ProductStatus;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
```

---

## Validaciones UX del formulario (frontend)

```typescript
// features/catalog/product-form/product-form.component.ts

this.form = this.fb.group({
  name: ['', [
    Validators.required,
    Validators.maxLength(300)
  ]],
  slug: ['', [
    Validators.required,
    Validators.maxLength(200),
    Validators.pattern(/^[a-z0-9-]+$/)   // solo lowercase, números y guiones
  ]],
  sku: [null, [
    Validators.maxLength(64)
    // NO validar unicidad — eso lo hace el backend
  ]],
  description: [null, [
    Validators.maxLength(5000)
  ]],
  price: [0, [
    Validators.required,
    Validators.min(0)
    // NO calcular con impuestos — el backend maneja precios
  ]],
  stockOnHand: [0, [
    Validators.required,
    Validators.min(0),
    Validators.max(999999)
  ]],
});
```

**Qué NO validar en el frontend:**
```typescript
// ❌ Nunca hacer esto
async checkSlugExists(slug: string) { ... }   // unicidad es del backend
calculateFinalPrice(price: number) { ... }    // el backend calcula
validateTransition(from: ProductStatus, to: ProductStatus) { ... } // del backend
checkStockBeforePublish() { ... }             // del backend
```

---

## Endpoints y llamadas

### GET /api/products (lista admin)

```typescript
// features/catalog/product-list/product-list.component.ts

export class ProductListComponent {
  private http = inject(HttpClient);

  page     = signal(1);
  loading  = signal(false);
  products = signal<PagedResult<ProductAdminDto> | null>(null);

  loadProducts() {
    this.loading.set(true);
    this.http.get<PagedResult<ProductAdminDto>>('/api/products', {
      params: { page: this.page(), pageSize: 20 }
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next:  result => this.products.set(result),
        error: err    => this.handleError(err)
      });
  }

  onPageChange(newPage: number) {
    this.page.set(newPage);
    this.loadProducts();
  }
}
```

### POST /api/products (crear)

```typescript
createProduct(request: CreateProductRequest): Observable<ProductAdminDto> {
  return this.http.post<ProductAdminDto>('/api/products', request);
}

// En el componente:
onSubmit() {
  if (this.form.invalid) return;
  this.saving.set(true);

  this.productService.createProduct(this.form.value)
    .pipe(finalize(() => this.saving.set(false)))
    .subscribe({
      next: product => {
        this.toast.success('Producto creado correctamente.');
        this.router.navigate(['/products', product.id]);
      },
      error: err => this.handleApiError(err)
    });
}
```

### PUT /api/products/{id} (actualizar)

```typescript
updateProduct(id: string, request: UpdateProductRequest): Observable<ProductAdminDto> {
  return this.http.put<ProductAdminDto>(`/api/products/${id}`, request);
}
```

### PATCH /api/products/{id}/status (cambiar estado)

```typescript
changeStatus(id: string, status: ProductStatus): Observable<ProductAdminDto> {
  return this.http.patch<ProductAdminDto>(
    `/api/products/${id}/status`,
    { status } satisfies ChangeStatusRequest
  );
}

// Mostrar confirmación antes de archivar
async onArchive(product: ProductAdminDto) {
  const confirmed = await this.dialog.confirm(
    '¿Archivar producto?',
    'El producto dejará de ser visible en la tienda.'
  );
  if (!confirmed) return;

  this.changeStatus(product.id, 2).subscribe({
    next: () => this.toast.success('Producto archivado.'),
    error: err => this.handleApiError(err)
  });
}
```

### GET /api/public/products (catálogo público)

```typescript
// En Storefront — sin token de auth
getPublicProducts(page = 1, pageSize = 20): Observable<PagedResult<ProductPublicDto>> {
  return this.http.get<PagedResult<ProductPublicDto>>('/api/public/products', {
    params: { page, pageSize }
  });
}

getProductBySlug(slug: string): Observable<ProductPublicDto> {
  return this.http.get<ProductPublicDto>(`/api/public/products/${slug}`);
}
```

---

## Manejo de errores específicos

```typescript
// features/catalog/product-form/product-form.component.ts

private handleApiError(err: HttpErrorResponse): void {
  const error = err.error as ErrorResponse;

  switch (error?.code) {
    case 'VALIDATION_ERROR':
      // Mostrar errores por campo si vienen en details
      this.applyServerValidationErrors(error.details);
      this.toast.error('Revisá los datos del formulario.');
      break;

    case 'CONFLICT':
      // Slug o SKU duplicado — backend encontró conflicto
      this.toast.error('Ya existe un producto con ese slug o SKU. Usá un valor diferente.');
      break;

    case 'NOT_FOUND':
      this.toast.error('Producto no encontrado.');
      this.router.navigate(['/products']);
      break;

    case 'PRODUCT_INVALID_STATE':
      this.toast.error('Este cambio de estado no está permitido.');
      break;

    case 'FORBIDDEN':
      this.toast.error('No tenés permisos para esta acción.');
      break;

    default:
      this.toast.error('Ocurrió un error inesperado. Intentá más tarde.');
  }
}

private applyServerValidationErrors(details: Record<string, string[]>): void {
  // details: { "Name": ["El nombre es obligatorio"], "Slug": [...] }
  Object.entries(details ?? {}).forEach(([field, messages]) => {
    const control = this.form.get(field.toLowerCase());
    if (control) {
      control.setErrors({ serverError: messages[0] });
    }
  });
}
```

---

## Errores posibles por endpoint

| Endpoint | Códigos posibles | Acción en UI |
|---|---|---|
| `POST /api/products` | `VALIDATION_ERROR`, `CONFLICT`, `UNAUTHORIZED`, `FORBIDDEN` | Ver manejo arriba |
| `PUT /api/products/{id}` | `VALIDATION_ERROR`, `CONFLICT`, `NOT_FOUND`, `FORBIDDEN` | Ídem |
| `PATCH /api/products/{id}/status` | `PRODUCT_INVALID_STATE`, `NOT_FOUND`, `FORBIDDEN` | Toast con mensaje específico |
| `GET /api/products` | `UNAUTHORIZED`, `FORBIDDEN` | Redirigir a login |
| `GET /api/public/products` | `TENANT_NOT_FOUND` | Pantalla de error 404 |
| `GET /api/public/products/{slug}` | `NOT_FOUND` | "Producto no encontrado" |

---

## Mapeo ProductStatus → UI

```typescript
export const PRODUCT_STATUS_LABEL: Record<ProductStatus, string> = {
  0: 'Borrador',
  1: 'Activo',
  2: 'Archivado',
};

export const PRODUCT_STATUS_BADGE: Record<ProductStatus, 'warning' | 'success' | 'info'> = {
  0: 'warning',   // Borrador → amarillo
  1: 'success',   // Activo   → verde
  2: 'info',      // Archivado → gris
};

// Transiciones permitidas para mostrar opciones en UI
// El backend decide si es válido — el frontend solo filtra opciones obvias
export const ALLOWED_TRANSITIONS: Record<ProductStatus, ProductStatus[]> = {
  0: [1],     // Draft → solo puede ir a Active
  1: [2],     // Active → solo puede ir a Archived
  2: [1],     // Archived → solo puede ir a Active
};
```
