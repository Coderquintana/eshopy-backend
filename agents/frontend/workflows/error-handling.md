# Workflow — Error Handling (Frontend)

> Cómo procesar y mostrar errores del backend. Centralizado y consistente.

---

## Estructura ErrorResponse del backend

```typescript
// core/models/api-error.models.ts
export interface ErrorResponse {
  traceId: string;
  code: string;        // ← usar para lógica de manejo
  message: string;     // ← NO mostrar al usuario directamente (puede estar en inglés o técnico)
  details?: Record<string, string[]>; // errores por campo (VALIDATION_ERROR)
}
```

**Regla de oro**: siempre usar `error.code` para la lógica. Nunca `error.message`.

---

## ErrorInterceptor (global)

```typescript
// core/interceptors/error.interceptor.ts
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast  = inject(ToastService);
  const router = inject(Router);
  const auth   = inject(AuthService).isLoggedIn; // solo en Admin

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const code = err.error?.code as string | undefined;

      switch (err.status) {
        case 401:
          // Token expirado o ausente → redirigir a login (solo Admin)
          if (typeof auth === 'function' && auth()) {
            toast.error('Tu sesión expiró. Por favor, volvé a iniciar sesión.');
            router.navigate(['/login']);
          }
          break;

        case 403:
          toast.error('No tenés permisos para realizar esta acción.');
          break;

        case 0: // Sin conexión / servidor caído
          toast.error('Sin conexión. Verificá tu internet e intentá nuevamente.');
          break;

        case 502:
        case 503:
          toast.error('El servicio no está disponible temporalmente. Intentá en unos minutos.');
          break;

        // Para el resto (400, 404, 409, 500): manejar en cada componente
      }

      // Re-lanzar siempre para que el componente pueda manejar casos específicos
      return throwError(() => err);
    })
  );
};
```

---

## Mapeo canónico: código → mensaje

```typescript
// core/utils/error-messages.ts
export const ERROR_MESSAGES: Record<string, string> = {
  // Genéricos
  VALIDATION_ERROR:        'Revisá los datos del formulario.',
  NOT_FOUND:               'No encontramos lo que buscás.',
  CONFLICT:                'Ya existe un elemento con esos datos.',
  UNAUTHORIZED:            'Necesitás iniciar sesión.',
  FORBIDDEN:               'No tenés permisos para esta acción.',
  GENERIC_ERROR:           'Ocurrió un error inesperado. Intentá más tarde.',
  TENANT_NOT_FOUND:        'Esta tienda no existe.',

  // Products
  PRODUCT_NOT_AVAILABLE:   'Este producto no está disponible.',
  PRODUCT_INVALID_STATE:   'Este cambio de estado no está permitido.',

  // Orders
  ORDER_INVALID_STATE:     'El pedido no puede cambiar a ese estado.',

  // Payments
  PAYMENT_WEBHOOK_INVALID: 'Error en la verificación del pago.',
  PAYMENT_PROVIDER_ERROR:  'No pudimos conectar con el sistema de pago. Intentá más tarde.',
};

export function getErrorMessage(code: string): string {
  return ERROR_MESSAGES[code] ?? ERROR_MESSAGES['GENERIC_ERROR'];
}
```

---

## Manejo en componentes — patrones

### Patrón 1: Error simple (toast)

```typescript
// Para operaciones que no tienen formulario
this.service.changeStatus(id, status).subscribe({
  next:  ()  => this.toast.success('Estado actualizado.'),
  error: err => this.toast.error(getErrorMessage(err.error?.code))
});
```

### Patrón 2: VALIDATION_ERROR con errores por campo

```typescript
// Para formularios — los errores del backend se aplican a los controles
private applyValidationErrors(details: Record<string, string[]>): void {
  // details: { "Name": ["El nombre es obligatorio."], "Slug": ["El slug ya existe."] }
  Object.entries(details ?? {}).forEach(([field, errors]) => {
    // Backend manda PascalCase, form tiene camelCase
    const controlName = field.charAt(0).toLowerCase() + field.slice(1);
    const control = this.form.get(controlName);
    if (control) {
      control.setErrors({ serverError: errors[0] });
      control.markAsTouched();
    }
  });
}

private handleApiError(err: HttpErrorResponse): void {
  const error = err.error as ErrorResponse;

  if (error?.code === 'VALIDATION_ERROR' && error.details) {
    this.applyValidationErrors(error.details as Record<string, string[]>);
    this.toast.error('Revisá los datos del formulario.');
  } else {
    this.toast.error(getErrorMessage(error?.code));
  }
}
```

### Patrón 3: Navegación en error

```typescript
// Para recursos no encontrados
this.service.getById(id).subscribe({
  next:  product => this.product.set(product),
  error: err     => {
    if (err.error?.code === 'NOT_FOUND') {
      this.toast.error('Producto no encontrado.');
      this.router.navigate(['/products']);
    } else {
      this.toast.error(getErrorMessage(err.error?.code));
    }
  }
});
```

### Patrón 4: Error en operación crítica (checkout)

```typescript
// Cuando el error requiere una acción del usuario
this.orderService.checkout(request).subscribe({
  error: err => {
    const code = err.error?.code;

    if (code === 'PRODUCT_NOT_AVAILABLE') {
      // Necesita volver al carrito — acción requerida
      this.toast.error(
        'Algunos productos ya no están disponibles. Revisá tu carrito.'
      );
      // No redirigir automáticamente — dejar que el usuario decida
    } else {
      this.toast.error(getErrorMessage(code));
    }
  }
});
```

---

## AppTextField — mostrar errores del servidor

```typescript
// En el template de AppTextField (interno al componente)
// El componente lee los errores del FormControl automáticamente

get errorMessage(): string | null {
  const control = this.ngControl?.control;
  if (!control?.touched || !control?.errors) return null;

  const errors = control.errors;
  if (errors['serverError']) return errors['serverError'];  // ← error del backend
  if (errors['required'])    return 'Este campo es obligatorio.';
  if (errors['email'])       return 'Ingresá un email válido.';
  if (errors['minlength'])   return `Mínimo ${errors['minlength'].requiredLength} caracteres.`;
  if (errors['maxlength'])   return `Máximo ${errors['maxlength'].requiredLength} caracteres.`;
  if (errors['min'])         return `El valor mínimo es ${errors['min'].min}.`;
  if (errors['max'])         return `El valor máximo es ${errors['max'].max}.`;
  if (errors['pattern'])     return 'Formato inválido.';
  return 'Campo inválido.';
}
```

---

## Pantallas de error global

```typescript
// Para errores que impiden mostrar la página (no toasts)

// 404 — Recurso no encontrado (ruta o producto)
// Mostrar: "No encontramos lo que buscás" + botón volver

// 403 — Sin permisos
// Mostrar: "No tenés permisos para esta sección"

// TENANT_NOT_FOUND — Subdominio inválido
// Mostrar: "Esta tienda no existe" (pantalla de error de app)

// Sin conexión (status 0)
// Mostrar: toast persistente "Sin conexión"
```

---

## CorrelationId — para soporte

```typescript
// core/interceptors/correlation.interceptor.ts
// Agrega X-Correlation-Id a todos los requests para trazabilidad

export const correlationInterceptor: HttpInterceptorFn = (req, next) => {
  const correlationId = crypto.randomUUID();
  return next(req.clone({
    setHeaders: { 'X-Correlation-Id': correlationId }
  })).pipe(
    tap(event => {
      // Si la respuesta tiene el header, guardar para mostrar en errores
      if (event instanceof HttpResponse) {
        const responseCorrelationId = event.headers.get('X-Correlation-Id');
        // Guardar en servicio de diagnóstico opcional
      }
    })
  );
};
```

---

## Tabla resumen: qué maneja el interceptor vs el componente

| Error | Manejado por | Acción |
|---|---|---|
| 401 (token expirado) | ErrorInterceptor | Toast + redirect a login |
| 403 (sin permisos) | ErrorInterceptor | Toast genérico |
| 0 (sin conexión) | ErrorInterceptor | Toast persistente |
| 502/503 | ErrorInterceptor | Toast genérico de servidor |
| 400 `VALIDATION_ERROR` | Componente | Errores por campo + toast |
| 404 `NOT_FOUND` | Componente | Toast + navigate o pantalla de error |
| 409 `CONFLICT` | Componente | Toast específico por contexto |
| 409 `PRODUCT_INVALID_STATE` | Componente | Toast con explicación |
| 409 `PRODUCT_NOT_AVAILABLE` | Componente | Toast + acción (ej: volver al carrito) |
| 502 `PAYMENT_PROVIDER_ERROR` | Componente | Toast con instrucciones de reintento |
| 500 `GENERIC_ERROR` | ErrorInterceptor o Componente | Toast genérico |
