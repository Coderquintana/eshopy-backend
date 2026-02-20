# Workflow — Product Management (Admin)

> CRUD de productos desde el Admin Panel. Con código TypeScript completo.

---

## Pantallas involucradas

| Pantalla | Ruta | Componente |
|---|---|---|
| Lista de productos | `/products` | `ProductListComponent` |
| Crear producto | `/products/new` | `ProductFormComponent` |
| Editar producto | `/products/:id` | `ProductFormComponent` |
| (Modal) Cambiar estado | Modal en lista | `ProductStatusDialogComponent` |

---

## ProductService (singleton en feature)

```typescript
// features/catalog/product.service.ts
@Injectable()
export class ProductService {
  private http = inject(HttpClient);

  getProducts(page: number, pageSize = 20): Observable<PagedResult<ProductAdminDto>> {
    return this.http.get<PagedResult<ProductAdminDto>>('/api/products', {
      params: { page, pageSize }
    });
  }

  getById(id: string): Observable<ProductAdminDto> {
    return this.http.get<ProductAdminDto>(`/api/products/${id}`);
  }

  create(request: CreateProductRequest): Observable<ProductAdminDto> {
    return this.http.post<ProductAdminDto>('/api/products', request);
  }

  update(id: string, request: UpdateProductRequest): Observable<ProductAdminDto> {
    return this.http.put<ProductAdminDto>(`/api/products/${id}`, request);
  }

  changeStatus(id: string, status: ProductStatus): Observable<ProductAdminDto> {
    return this.http.patch<ProductAdminDto>(`/api/products/${id}/status`, { status });
  }
}
```

---

## Flujo 1: Ver lista de productos

```typescript
// features/catalog/product-list/product-list.component.ts
@Component({
  standalone: true,
  providers: [ProductService],
  template: `
    <app-page-layout pageTitle="Catálogo de productos">
      <ng-container slot="actions">
        <app-button routerLink="new">+ Nuevo producto</app-button>
      </ng-container>

      <app-data-grid
        [columns]="columns"
        [rows]="result()?.items ?? []"
        [loading]="loading()"
        [totalCount]="result()?.totalCount ?? 0"
        [page]="page()"
        (pageChange)="onPageChange($event)"
        (rowClick)="onEdit($event)"
      >
        <!-- Template para columna Estado -->
        <ng-template #statusCell let-product>
          <app-badge
            [status]="PRODUCT_STATUS_BADGE[product.status]"
            [label]="PRODUCT_STATUS_LABEL[product.status]"
          />
        </ng-template>

        <!-- Template para columna Precio -->
        <ng-template #priceCell let-product>
          {{ formatPYG(product.price) }}
        </ng-template>

        <!-- Template para columna Acciones -->
        <ng-template #actionsCell let-product>
          <app-button variant="ghost" (click)="onChangeStatus(product, $event)">
            Cambiar estado
          </app-button>
        </ng-template>
      </app-data-grid>
    </app-page-layout>
  `
})
export class ProductListComponent implements OnInit {
  private service = inject(ProductService);
  private router  = inject(Router);
  private toast   = inject(ToastService);

  page    = signal(1);
  loading = signal(false);
  result  = signal<PagedResult<ProductAdminDto> | null>(null);

  columns: GridColumn[] = [
    { key: 'name',        label: 'Nombre',  sortable: true },
    { key: 'sku',         label: 'SKU' },
    { key: 'price',       label: 'Precio',  template: 'priceCell' },
    { key: 'stockOnHand', label: 'Stock' },
    { key: 'status',      label: 'Estado',  template: 'statusCell' },
    { key: 'actions',     label: '',        template: 'actionsCell' },
  ];

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.service.getProducts(this.page())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next:  r   => this.result.set(r),
        error: err => this.toast.error('No pudimos cargar los productos.')
      });
  }

  onPageChange(p: number) { this.page.set(p); this.load(); }
  onEdit(product: ProductAdminDto) { this.router.navigate(['/products', product.id]); }
}
```

---

## Flujo 2: Crear producto

```typescript
// features/catalog/product-form/product-form.component.ts
@Component({
  standalone: true,
  providers: [ProductService],
  template: `
    <app-page-layout [pageTitle]="isEdit() ? 'Editar producto' : 'Nuevo producto'" [showBackButton]="true">
      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <app-text-field label="Nombre *" formControlName="name" />
        <div class="row">
          <app-text-field label="Slug *" formControlName="slug"
            hint="Solo letras minúsculas, números y guiones. Ej: remera-blanca"
            [readonly]="isEdit()" />
          <app-text-field label="SKU" formControlName="sku"
            hint="Opcional. Código interno del producto." />
        </div>
        <app-text-field label="Descripción" formControlName="description"
          type="textarea" />
        <div class="row">
          <app-text-field label="Precio *" formControlName="price"
            type="number" prefix="₲" hint="Sin decimales." />
          <app-text-field label="Stock *" formControlName="stockOnHand"
            type="number" hint="Cantidad disponible." />
        </div>

        <div class="form-actions">
          <app-button variant="secondary" routerLink="/products">Cancelar</app-button>
          <app-button type="submit" [disabled]="form.invalid" [loading]="saving()">
            {{ isEdit() ? 'Guardar cambios' : 'Crear producto' }}
          </app-button>
        </div>
      </form>
    </app-page-layout>
  `
})
export class ProductFormComponent implements OnInit {
  private fb      = inject(FormBuilder);
  private service = inject(ProductService);
  private router  = inject(Router);
  private route   = inject(ActivatedRoute);
  private toast   = inject(ToastService);

  saving = signal(false);
  isEdit = signal(false);
  productId = signal<string | null>(null);

  form = this.fb.group({
    name:         ['', [Validators.required, Validators.maxLength(300)]],
    slug:         ['', [Validators.required, Validators.maxLength(200), Validators.pattern(/^[a-z0-9-]+$/)]],
    sku:          [null as string | null, [Validators.maxLength(64)]],
    description:  [null as string | null, [Validators.maxLength(5000)]],
    price:        [0, [Validators.required, Validators.min(0)]],
    stockOnHand:  [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit() {
    const id = this.route.snapshot.params['id'];
    if (id) {
      this.isEdit.set(true);
      this.productId.set(id);
      this.loadProduct(id);
    }
  }

  private loadProduct(id: string) {
    this.service.getById(id).subscribe({
      next: p => this.form.patchValue({
        name: p.name, slug: p.slug, sku: p.sku,
        description: p.description, price: p.price, stockOnHand: p.stockOnHand
      }),
      error: () => { this.toast.error('Producto no encontrado.'); this.router.navigate(['/products']); }
    });
  }

  onSubmit() {
    if (this.form.invalid) return;
    this.saving.set(true);

    const action$ = this.isEdit()
      ? this.service.update(this.productId()!, this.form.value as UpdateProductRequest)
      : this.service.create(this.form.value as CreateProductRequest);

    action$.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.toast.success(this.isEdit() ? 'Producto actualizado.' : 'Producto creado.');
        this.router.navigate(['/products']);
      },
      error: err => this.handleError(err)
    });
  }

  private handleError(err: HttpErrorResponse) {
    const code = err.error?.code;
    if (code === 'CONFLICT')
      this.toast.error('Ya existe un producto con ese slug o SKU.');
    else if (code === 'VALIDATION_ERROR')
      this.toast.error('Revisá los datos del formulario.');
    else
      this.toast.error('No pudimos guardar el producto. Intentá más tarde.');
  }
}
```

---

## Flujo 3: Cambiar estado de producto

```typescript
// Desde la lista, al hacer click en "Cambiar estado":
async onChangeStatus(product: ProductAdminDto, event: Event) {
  event.stopPropagation(); // no navegar al editar

  // Mostrar opciones según las transiciones permitidas
  const nextStatuses = ALLOWED_TRANSITIONS[product.status];
  const selectedStatus = await this.dialog.select(
    `Cambiar estado de "${product.name}"`,
    nextStatuses.map(s => ({ value: s, label: PRODUCT_STATUS_LABEL[s] }))
  );

  if (selectedStatus === null) return; // canceló

  // Confirmar si es archivar
  if (selectedStatus === 2) {
    const confirmed = await this.dialog.confirm(
      'Archivar producto',
      'El producto dejará de ser visible en tu tienda. Podés reactivarlo después.'
    );
    if (!confirmed) return;
  }

  this.service.changeStatus(product.id, selectedStatus).subscribe({
    next: () => {
      this.toast.success(`Estado cambiado a "${PRODUCT_STATUS_LABEL[selectedStatus]}".`);
      this.load(); // recargar lista
    },
    error: err => {
      const code = err.error?.code;
      if (code === 'PRODUCT_INVALID_STATE')
        this.toast.error('Este cambio de estado no está permitido.');
      else
        this.toast.error('No pudimos cambiar el estado.');
    }
  });
}
```

---

## Notas de UX

| Comportamiento | Implementación |
|---|---|
| Slug es readonly al editar | `[readonly]="isEdit()"` en el campo slug |
| Slug se auto-genera al escribir el nombre (al crear) | `effect(() => { if (!this.isEdit()) this.form.patchValue({ slug: slugify(this.name()) }) })` |
| Draft no puede archivarse | `ALLOWED_TRANSITIONS[0] = [1]` — solo muestra "Activar" |
| Confirmar antes de archivar | Dialog de confirmación con descripción del impacto |
| Precio en guaraníes sin decimales | `type="number"` + `Validators.min(0)` + `step="1"` |
| Error de backend por campo | `control.setErrors({ serverError: mensaje })` |
