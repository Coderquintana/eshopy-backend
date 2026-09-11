# AGENTS.md

Instrucciones para agentes IA (Claude Code, Codex) trabajando en `eshopy-backend`.

## Primero

Documentación viva y completa: [`agents/README.md`](agents/README.md) — léelo antes de tocar código.
Ahí está la navegación por tarea, el estado real (`agents/backend/CURRENT_STATE.md`), el backlog
(`agents/backend/BACKLOG.md`) y las decisiones firmes de arquitectura (`agents/backend/GOVERNANCE.md`).

## Reglas que no se negocian

1. `TenantId` nunca sale del frontend ni viene del body del request — se resuelve del subdominio en middleware.
2. Código en inglés; docs y comentarios en español.
3. Todo cambio de arquitectura no trivial se documenta en `agents/backend/GOVERNANCE.md` antes de repetirse.
4. Cambio en un endpoint → actualizar `Documentation/Postman/`.
5. Columna EF Core nueva → siempre con `HasComment()` en la configuración.
6. Commits: Conventional Commits, descripción en inglés.
7. Al terminar una tarea: actualizar `agents/backend/CURRENT_STATE.md` y `agents/backend/BACKLOG.md`.

## Levantar el entorno

Seguir [`.claude/skills/run-eshopy/SKILL.md`](.claude/skills/run-eshopy/SKILL.md) al pie de la letra
(orden de arranque, puerto de la API, el truco del header `Host`). El "Quick start" de
[`README.md`](README.md) es la versión corta para un smoke test sin frontend.
