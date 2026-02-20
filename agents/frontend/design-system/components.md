# Design System — Componentes

> Specs de cada componente en `libs/ui`. Usar SIEMPRE estos en vez de HTML nativo.

## Regla general

```typescript
// Todos los componentes de libs/ui son:
@Component({
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  // selector: 'app-<nombre>'
})
```

---

## AppButton

```typescript
// Selector: <app-button>
// Archivo: libs/ui/src/button/app-button.component.ts

@Input() variant: 'primary' | 'secondary' | 'ghost' | 'danger' = 'primary';
@Input() size: 'sm' | 'md' | 'lg' = 'md';
@Input() disabled = false;
@Input() loading = false;    // muestra spinner, deshabilita el botón
@Input() type: 'button' | 'submit' | 'reset' = 'button';
@Input() fullWidth = false;
```

```html
<!-- Uso -->
<app-button variant="primary" (click)="save()">Guardar</app-button>
<app-button variant="secondary" routerLink="/products">Cancelar</app-button>
<app-button variant="danger" [loading]="deleting()" (click)="delete()">Eliminar</app-button>
<app-button type="submit" [disabled]="form.invalid" [loading]="saving()">
  Crear producto
</app-button>
```

| Variant | Uso |
|---|---|
| `primary` | Acción principal de la página |
| `secondary` | Acción secundaria / cancelar |
| `ghost` | Acciones terciarias, navegación |
| `danger` | Acciones destructivas (eliminar, archivar) |

---

## AppTextField

```typescript
// Selector: <app-text-field>
// Wrapper de input con label, hint y mensajes de error

@Input() label = '';
@Input() placeholder = '';
@Input() type: 'text' | 'email' | 'number' | 'password' | 'search' | 'url' = 'text';
@Input() hint = '';              // texto de ayuda bajo el campo
@Input() prefix = '';            // ej: "$" antes del input
@Input() suffix = '';            // ej: "PYG" después del input
@Input() readonly = false;
@Input() required = false;       // solo muestra asterisco — validación via FormControl
// Integra con Reactive Forms via ControlValueAccessor
```

```html
<!-- Uso con Reactive Forms -->
<app-text-field
  label="Nombre del producto"
  placeholder="Ej: Remera Blanca"
  [required]="true"
  formControlName="name"
/>

<app-text-field
  label="Precio"
  type="number"
  prefix="₲"
  hint="Precio en guaraníes. Sin decimales."
  formControlName="price"
/>

<app-text-field
  label="SKU"
  placeholder="Ej: REM-001"
  hint="Opcional. Se normaliza a mayúsculas."
  formControlName="sku"
/>
```

**Errores automáticos** — el componente muestra errores del FormControl mapeados:

```typescript
// Mensajes por tipo de error (internos al componente)
const ERROR_MESSAGES: Record<string, string> = {
  required:  'Este campo es obligatorio.',
  email:     'Ingresá un email válido.',
  minlength: 'Mínimo {requiredLength} caracteres.',
  maxlength: 'Máximo {requiredLength} caracteres.',
  min:       'El valor mínimo es {min}.',
  max:       'El valor máximo es {max}.',
  pattern:   'Formato inválido.',
};
```

---

## AppSelect

```typescript
// Selector: <app-select>
@Input() label = '';
@Input() options: { value: unknown; label: string }[] = [];
@Input() placeholder = 'Seleccioná una opción';
@Input() required = false;
// Integra con Reactive Forms via ControlValueAccessor
```

```html
<app-select
  label="Estado"
  [options]="statusOptions()"
  formControlName="status"
/>
```

```typescript
// En el componente
statusOptions = signal([
  { value: 0, label: 'Borrador' },
  { value: 1, label: 'Activo' },
  { value: 2, label: 'Archivado' },
]);
```

---

## AppToast

```typescript
// Servicio: ToastService (inyectar, no usar componente directamente)
// El componente AppToastContainer se coloca en el AppComponent raíz

@Injectable({ providedIn: 'root' })
export class ToastService {
  success(message: string, duration = 3000): void
  error(message: string, duration = 5000): void
  warning(message: string, duration = 4000): void
  info(message: string, duration = 3000): void
}
```

```typescript
// Uso en componente
constructor(private toast: ToastService) {}

onSave() {
  this.productService.create(this.form.value).subscribe({
    next: () => this.toast.success('Producto creado correctamente.'),
    error: (err) => this.toast.error(this.errorMessages[err.error?.code] ?? 'Error al crear el producto.')
  });
}
```

---

## AppDialog

```typescript
// Selector: <app-dialog> — wrapping modal
@Input() title = '';
@Input() open = false;
@Output() closed = new EventEmitter<void>();
```

```html
<app-dialog title="¿Eliminar producto?" [open]="showDeleteDialog()">
  <p>Esta acción no se puede deshacer.</p>
  <ng-container slot="actions">
    <app-button variant="secondary" (click)="closeDialog()">Cancelar</app-button>
    <app-button variant="danger" [loading]="deleting()" (click)="confirmDelete()">
      Eliminar
    </app-button>
  </ng-container>
</app-dialog>
```

---

## AppDataGrid

```typescript
// Selector: <app-data-grid>
@Input() columns: GridColumn[] = [];
@Input() rows: unknown[] = [];
@Input() loading = false;
@Input() totalCount = 0;
@Input() page = 1;
@Input() pageSize = 20;
@Output() pageChange = new EventEmitter<number>();
@Output() rowClick = new EventEmitter<unknown>();

interface GridColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  template?: TemplateRef<unknown>; // para celdas customizadas
}
```

```html
<app-data-grid
  [columns]="columns"
  [rows]="products()"
  [loading]="loading()"
  [totalCount]="totalCount()"
  [page]="currentPage()"
  [pageSize]="20"
  (pageChange)="onPageChange($event)"
  (rowClick)="onProductClick($event)"
/>
```

---

## AppLoading

```html
<!-- Spinner de carga inline -->
<app-loading size="sm" />
<app-loading size="md" />  <!-- default -->
<app-loading size="lg" />

<!-- Skeleton de tabla (mientras carga lista) -->
<app-loading variant="table" [rows]="5" />

<!-- Skeleton de card (mientras carga catálogo) -->
<app-loading variant="card" />
```

---

## AppBadge / AppChip (estado de producto)

```typescript
// Selector: <app-badge>
@Input() status: 'draft' | 'active' | 'archived' | 'success' | 'error' | 'warning' | 'info' = 'info';
@Input() label = '';
```

```html
<!-- Mapeo de ProductStatus → badge -->
<app-badge
  [status]="product.status === 1 ? 'active' : product.status === 0 ? 'draft' : 'archived'"
  [label]="product.status === 1 ? 'Activo' : product.status === 0 ? 'Borrador' : 'Archivado'"
/>
```

| Status backend | Variant badge | Label español |
|---|---|---|
| `Draft` (0) | `draft` / `warning` | "Borrador" |
| `Active` (1) | `active` / `success` | "Activo" |
| `Archived` (2) | `archived` / `info` | "Archivado" |

---

## AppPageLayout

Ver `design-system/layouts.md` para el uso de este componente estructural.

---

## Barrel export

```typescript
// libs/ui/src/index.ts
export * from './button/app-button.component';
export * from './text-field/app-text-field.component';
export * from './select/app-select.component';
export * from './toast/toast.service';
export * from './toast/app-toast-container.component';
export * from './dialog/app-dialog.component';
export * from './data-grid/app-data-grid.component';
export * from './loading/app-loading.component';
export * from './badge/app-badge.component';
export * from './page-layout/app-page-layout.component';
```
