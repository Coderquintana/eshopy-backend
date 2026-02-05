# Tasks

## Completed
- Backend skeleton
- Base documentation
- Products module

## In Progress

## Pending
- Cart module
- Orders module
- Payments module
- EF Core integration
- Keycloak integration

---

# Historial de cambios

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
