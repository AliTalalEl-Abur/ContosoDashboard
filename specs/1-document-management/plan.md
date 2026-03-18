# Implementation Plan: Document Upload and Management

**Branch**: `1-document-management` | **Date**: 2026-03-18 | **Spec**: `/specs/1-document-management/spec.md`
**Input**: Feature specification from `/specs/1-document-management/spec.md`

## Summary

Deliver a complete document management capability for ContosoDashboard: secure upload,
owner and project-scoped discovery, controlled sharing, lifecycle actions (edit,
replace, delete), and integration into dashboard/task views.

The implementation is storage-sensitive and security-sensitive because it introduces
binary file persistence and direct file retrieval paths. The design enforces role,
ownership, and project-membership checks at UI and service layers, with GUID-based
internal file paths and non-webroot storage.

## Technical Context

**Language/Version**: C# / ASP.NET Core 9 / Blazor Server
**Primary Dependencies**: ASP.NET Core Razor Pages + Blazor Server, Entity Framework Core, SQL Server provider, Bootstrap 5
**Storage**: SQL Server LocalDB for metadata; local filesystem outside `wwwroot` for uploaded files
**Testing**: Reproducible manual verification required; add focused automated tests where logic is isolated (`DocumentService` validation/auth rules)
**Target Platform**: Local developer workstation, offline-capable training environment
**Project Type**: Single web application (`ContosoDashboard/`)
**Performance Goals**:
- Upload of a file <= 25 MB completes in <= 30 seconds (SC-002)
- My Documents list load <= 2 seconds for up to 500 docs (SC-003)
- Search response <= 2 seconds for up to 500 accessible docs (SC-004)
**Constraints**: Training-only scope, offline-first behavior, role-based authorization, no major architectural rewrites, preserve cloud-migration seams via storage abstraction
**Scale/Scope**: One feature slice inside existing app, current roles only (Employee, Team Lead, Project Manager, Administrator)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- `Training-First, Production-Aware Boundaries`: PASS. The plan keeps implementation local and explicit about deferred production concerns (virus scanning, CDN, version UI).
- `Offline-First, Abstraction-Led Infrastructure`: PASS. File operations are behind `IFileStorageService`; default implementation is local filesystem.
- `Authorization and Data Isolation by Default`: PASS. Service-layer authorization is mandatory for file retrieval and protected operations; unauthorized access returns 403.
- `Spec-Driven Vertical Slices`: PASS. Work is grouped by independent user-story slices (upload, browse, project access, search, lifecycle, sharing, integration).
- `Verification with Reproducible Evidence`: PASS. Plan includes manual security/validation checks and targeted future automation points.

## Project Structure

### Documentation (this feature)

```text
specs/1-document-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── document-management.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
├── Models/
├── Pages/
├── Services/
├── Shared/
├── wwwroot/
├── Program.cs
└── ContosoDashboard.csproj
```

**Structure Decision**: Use the existing single-project Blazor Server structure. Put data models in `Models/`, EF wiring in `Data/`, business logic in `Services/`, routed pages in `Pages/`, and navigation/widgets in `Shared/`.

## Phase 0: Research Outcomes

Research artifact: `/specs/1-document-management/research.md`

Resolved decisions for implementation:

1. Local file storage outside `wwwroot` with relative persisted paths.
2. Upload transaction pattern: write file first, then metadata; cleanup file on DB failure.
3. Authorization pattern: central `DocumentAccessService` checks for owner/project/share/admin before serve/edit/delete.
4. Sharing persistence: support individual and project-level recipients through `DocumentShare` relations and query-time membership checks.
5. Activity logging: append-only audit table and admin-only filtered page.
6. Search strategy: indexed metadata filtering with server-side pagination and deterministic sort.

No unresolved clarifications remain for planning.

## Phase 1: Design and Contracts

Design artifacts:
- Data model: `/specs/1-document-management/data-model.md`
- API contract: `/specs/1-document-management/contracts/document-management.openapi.yaml`
- Validation/run guide: `/specs/1-document-management/quickstart.md`

Planned implementation slices:

1. Foundation and persistence
- Add models: `Document`, `DocumentShare`, `DocumentActivity`, optional `TaskDocument` bridge.
- Add EF DbSet entries and migration.
- Add indexes for uploader, project, uploaded timestamp, and title/category search fields.

2. Storage abstraction
- Add `IFileStorageService` and `LocalFileStorageService`.
- Enforce extension allowlist and size limit before persistence.
- Generate GUID-based storage names and relative paths.

3. Core services
- `DocumentService`: upload/list/filter/search/update/replace/delete.
- `DocumentAuthorizationService`: owner/project/share/admin checks.
- `DocumentActivityService`: audit append and admin queries.

4. UI integration
- New pages/components: document upload/list/search/shared views.
- Add project details document section and task attach/view integration.
- Add dashboard widget for recent documents and document count card.

5. Admin and sharing
- Share workflow and notifications integration.
- Admin-only activity log page with filters (actor/action/date/document).

## Phase 2: Implementation Planning (Story-first)

- Story P1/P2: Upload + My Documents sorting/filtering
- Story P3/P4: Project access + Search
- Story P5: Edit/replace/delete lifecycle
- Story P6: Share + Shared with Me + notifications
- Story P7: Dashboard/task integration + admin activity visibility

Each story has independent validation scenarios and security-negative tests.

## Verification Strategy

Manual evidence to capture during implementation:
- File type/size rejection behavior
- IDOR denial behavior on direct URL access
- Project membership authorization behavior
- Share recipient visibility behavior
- Activity log correctness and role restriction

Targeted automation candidates (if harness added):
- `DocumentService` validation and cleanup logic
- `DocumentAuthorizationService` permission matrix tests
- Upload->persist->retrieve integration happy path

## Post-Design Constitution Re-Check

- `Training-First, Production-Aware Boundaries`: PASS
- `Offline-First, Abstraction-Led Infrastructure`: PASS
- `Authorization and Data Isolation by Default`: PASS
- `Spec-Driven Vertical Slices`: PASS
- `Verification with Reproducible Evidence`: PASS

## Complexity Tracking

No constitution violations identified. Table intentionally left empty.
