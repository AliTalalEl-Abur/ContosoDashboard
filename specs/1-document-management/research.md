# Research: Document Upload and Management

## Decision 1: Storage location and path format

- Decision: Store files outside `wwwroot` under an app-owned root (for example `AppData/uploads/`) and persist only relative GUID-based paths in DB.
- Rationale: Prevents direct URL access and supports root relocation without data migration.
- Alternatives considered:
  - Store under `wwwroot/uploads`: rejected due to public path exposure and weaker access control.
  - Store absolute paths in DB: rejected due to environment coupling and portability risk.

## Decision 2: Upload consistency and failure handling

- Decision: Apply file-first then metadata transaction pattern, with cleanup on metadata failure.
- Rationale: Aligns with FR-008 and prevents orphan metadata records.
- Alternatives considered:
  - Metadata first then file write: rejected due to orphan metadata risk when file write fails.
  - Eventual consistency queue: rejected as unnecessary complexity for training scope.

## Decision 3: Authorization enforcement layer

- Decision: Enforce permission checks in service layer for all read/write operations, including direct download endpoint calls.
- Rationale: UI-only checks are bypassable; service checks are required for IDOR resistance.
- Alternatives considered:
  - Razor page-level checks only: rejected as insufficient for API/direct route access.
  - DB row-level security: rejected for added complexity in training environment.

## Decision 4: Sharing model

- Decision: Model sharing with `DocumentShare` supporting either user-target or project-target recipient semantics.
- Rationale: Supports individual and team sharing without duplicating document records.
- Alternatives considered:
  - Duplicate document rows per recipient: rejected due to synchronization and ownership ambiguity.
  - Team share fan-out row per user at write-time: rejected due to recipient drift as project membership changes.

## Decision 5: Audit visibility and scope

- Decision: Record all activity in `DocumentActivity` and expose via admin-only activity page with filters.
- Rationale: Provides traceability and keeps operational oversight isolated from user notification flows.
- Alternatives considered:
  - Notifications-only surface for admins: rejected due to weak audit usability.
  - Persist logs without UI: rejected because FR-019 requires admin accessibility.

## Decision 6: Search and performance

- Decision: Use server-side filtered query with pagination and indexed fields (title, uploader, project, uploaded date).
- Rationale: Meets <=2s target at defined scale and avoids loading full result sets into memory.
- Alternatives considered:
  - Client-side filtering from full fetch: rejected for scale/performance.
  - Full-text external engine: rejected as out of scope and non-offline-friendly.

## Decision 7: Validation strategy

- Decision: Validate file extension allowlist, max size, and required metadata on server side before write.
- Rationale: Server-side validation is required regardless of client behavior and aligns with FR-002/FR-003/FR-004.
- Alternatives considered:
  - Client-only validation: rejected as bypassable.
  - MIME-only allowlist: rejected due to spoofing risk; extension + MIME consistency checks preferred.

## Clarification Resolution Summary

No `NEEDS CLARIFICATION` placeholders remain for planning artifacts. The implementation can proceed to design and task decomposition.
