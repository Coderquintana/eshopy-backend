# TASKS

Este archivo es el punto de entrada para todas las IAs (ChatGPT, Codex, Copilot).
Objetivo: dejar contexto operativo, decisiones y próximos pasos de manera concisa.

## Mensaje para IAs
- Leer primero este archivo antes de tocar código.
- Respetar la arquitectura (monolito modular + Clean Architecture + vertical slices).
- Multi-tenant por subdominio/host; aislamiento por TenantId en datos.
- No exponer TenantId al frontend; se resuelve en backend.
- Mantener contratos alineados con OpenAPI.
- Priorizar consistencia sobre “innovación” en naming y estructuras existentes.

## Contexto resumido del proyecto
- Producto: eShopy (SaaS e-commerce, MVP Plan Básico).
- Stack: .NET 10 backend, Angular frontend, Keycloak para auth.
- Arquitectura: monolito modular con separación por dominios.
- Persistencia: modelo SQL Server definido; actualmente hay repositorios in-memory.
- Tests: unitarios + integración (smoke).

## Módulos y estado (alto nivel)
- Implementado: Products (dominio, aplicación, API, infraestructura, tests).
- Pendiente: Cart, Orders, Payments, EF Core, Keycloak.

## Fuentes de verdad (docs)
- `Documentation Copy/` contiene los documentos base.
- `documentation.md` es el compilado rápido (generado).
- `Documentation/Postman/EShopy_Backend_MVP.postman_collection.json` define endpoints MVP.

## Convenciones mínimas
- DTOs y requests en inglés; comentarios y documentación en español.
- Fechas en ISO 8601 UTC.
- Errores estandarizados (ErrorResponse).
- Evitar cambios masivos sin justificación.
- Todo cambio en endpoints debe reflejarse en la colección Postman.
- La colección Postman debe mantenerse completa y bien documentada.
- Las entidades deben tener descripción por columna (comentarios) para que EF genere columnas con descripción en DB.
- Si se agregan o modifican columnas, actualizar siempre sus descripciones.

## Postman
- Colección: `Documentation/Postman/EShopy_Backend_MVP.postman_collection.json`
- Entorno: `Documentation/Postman/EShopy_Backend_MVP.postman_environment.json`

## Encoding (muy importante)
- Todo el repo debe estar en UTF-8 (sin BOM).
- En Windows/PowerShell, `Get-Content` usa encoding del sistema y puede mostrar mojibake. Preferir `Get-Content -Encoding utf8`.
- Si ejecutas scripts (Python/PS), siempre leer/escribir con `encoding='utf-8'`.
- Evitar “arreglar” archivos re-guardando con otra codificación.
- El proyecto usa acentos y ñ (Paraguay). No eliminar ni reemplazar caracteres.

## Swagger / XML Docs
- Siempre agregar `<summary>` en controllers y actions.
- Mantener XML docs habilitado para Swagger (`GenerateDocumentationFile=true` + `IncludeXmlComments`).

## Seguridad (Keycloak)
- Login se realiza en Keycloak (no hay endpoint de login en backend).
- Backend valida JWT Bearer (OIDC) y usa policies por módulo.
- Permissions claim: `permissions` (roles mapeados a permisos).
- Cliente dev para Postman: `eshopy-postman` (secret `postman-secret`).

## EF Core / DB
- DB dev: `EShopy.Dev` en `localhost\\SQLEXPRESS`.
- Usar EF Core con SQL Server.
- Todas las columnas deben tener `HasComment` en el mapping.
- Mantener índices/constraints según documentación.
- Si SQL Browser no está activo, usar `lpc:` en la connection string para evitar error de instancia.

## Commits
- Mantener commits pequeños y temáticos.
- Formato sugerido: `type(scope): resumen corto`
- Tipos: feat, fix, docs, chore, refactor, test, build.
- Incluir detalle en el body cuando haya cambios de arquitectura/contratos.

# HISTORIAL DE CAMBIOS

## 2026-02-07
### Resumen
- Se consolidó documentación y se creó colección Postman base para endpoints MVP.
- Se corrigieron referencias de proyectos y se normalizó el build con .NET 10.
- Se añadió sección de Data JSON / typed data en modelo de datos.

### Documentación
- Se eliminaron versiones duplicadas y se dejaron las finales en `Documentation Copy`.
- Se actualizó consistencia de multi-tenant (resolución por subdominio/host).
- Se corrigieron contratos (SKU opcional, código de error de producto).
- Se generó `documentation.md` compilado.

### Tooling
- Se agregó `.gitignore` para artefactos de build.

### Build
- Se corrigieron rutas de ProjectReference en `EShopy.Api`, `EShopy.Application` y `EShopy.Infrastructure`.
- Se agregó referencia a `Microsoft.Extensions.DependencyInjection` en infraestructura.

## 2025-02-14
### Resumen
- Se implementó el módulo de Productos (dominio, aplicación, API, infraestructura y tests) siguiendo la arquitectura del backend.

### Arquitectura
- Se agregó el agregado `Product` con reglas de negocio, y el enum `ProductStatus`.
- Se mantuvo el enfoque multi-tenant por subdominio usando `TenantContext` y repositorio in-memory aislado por `TenantId`.

### Diseño y definiciones
- Contratos de transporte (DTOs) y requests alineados con OpenAPI.
- Validaciones de request con FluentValidation y reglas de dominio con `DomainException`.
- Endpoints admin y públicos para catálogo, sin exponer `tenantId`.

### Tests
- Tests unitarios para reglas de dominio y validadores.
- Smoke test de integración para flujo básico (crear, publicar, listar).
