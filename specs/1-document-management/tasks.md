# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/1-document-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are deferred by the current plan; this task list includes mandatory manual verification tasks for each story and for cross-cutting security/performance checks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Application code lives in `ContosoDashboard/`
- UI pages live in `ContosoDashboard/Pages/` and shared layout/navigation in `ContosoDashboard/Shared/`
- Business logic lives in `ContosoDashboard/Services/`
- Data models and persistence live in `ContosoDashboard/Models/` and `ContosoDashboard/Data/`
- Static assets live in `ContosoDashboard/wwwroot/`
- Feature planning artifacts live in `specs/1-document-management/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare configuration, routes, and feature entry points before persistence and story work starts.

- [X] T001 Review and reserve feature extension points in `ContosoDashboard/Program.cs`, `ContosoDashboard/Pages/ProjectDetails.razor`, `ContosoDashboard/Pages/Tasks.razor`, `ContosoDashboard/Pages/Index.razor`, and `ContosoDashboard/Shared/NavMenu.razor`
- [X] T002 Add document-storage configuration and limits to `ContosoDashboard/appsettings.json`, `ContosoDashboard/appsettings.Development.json`, and `ContosoDashboard/Program.cs`
- [X] T003 [P] Create feature file placeholders in `ContosoDashboard/Pages/Documents.razor`, `ContosoDashboard/Pages/DocumentActivity.razor`, `ContosoDashboard/Pages/DocumentDownload.cshtml`, `ContosoDashboard/Pages/DocumentDownload.cshtml.cs`, `ContosoDashboard/Shared/DocumentUploadForm.razor`, and `ContosoDashboard/Shared/DocumentList.razor`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core persistence, storage, authorization, and audit infrastructure that MUST be complete before ANY user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Create the document aggregate model in `ContosoDashboard/Models/Document.cs`
- [X] T005 [P] Create sharing and audit entities in `ContosoDashboard/Models/DocumentShare.cs` and `ContosoDashboard/Models/DocumentActivity.cs`
- [X] T006 [P] Add the task attachment bridge model in `ContosoDashboard/Models/TaskDocument.cs`
- [X] T007 Update EF persistence, relationships, and indexes in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T008 Add the EF Core migration for document tables and indexes in `ContosoDashboard/Migrations/`
- [X] T009 Create the storage seam in `ContosoDashboard/Services/IFileStorageService.cs` and `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T010 [P] Create centralized authorization logic in `ContosoDashboard/Services/DocumentAuthorizationService.cs`
- [X] T011 [P] Create audit logging/query support in `ContosoDashboard/Services/DocumentActivityService.cs`
- [X] T012 Create the core orchestration service shell in `ContosoDashboard/Services/DocumentService.cs`
- [X] T013 Register document services, options, and storage root wiring in `ContosoDashboard/Program.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Upload a Document (Priority: P1) 🎯 MVP

**Goal**: Let an authenticated user upload a supported document with required metadata and persist it safely outside `wwwroot`.

**Independent Test**: Log in as an employee, upload a valid PDF under 25 MB from the Documents page, and verify it appears in the user's document list with the expected metadata.

### Implementation for User Story 1

- [X] T014 [P] [US1] Add upload request and list item models in `ContosoDashboard/Models/DocumentUploadRequest.cs` and `ContosoDashboard/Models/DocumentListItem.cs`
- [X] T015 [US1] Implement upload validation, file-write ordering, and orphan cleanup in `ContosoDashboard/Services/DocumentService.cs`
- [X] T016 [P] [US1] Build the upload form component in `ContosoDashboard/Shared/DocumentUploadForm.razor`
- [X] T017 [US1] Create the initial document workspace page in `ContosoDashboard/Pages/Documents.razor`
- [X] T018 [US1] Surface the Documents entry point in `ContosoDashboard/Shared/NavMenu.razor`
- [ ] T019 [US1] Execute and record the upload/manual negative checks in `specs/1-document-management/quickstart.md`

**Checkpoint**: User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Browse and Filter My Documents (Priority: P2)

**Goal**: Let users list only their own documents and refine the list via sorting and filters.

**Independent Test**: Log in as a user with multiple uploads, verify only their documents appear, and confirm sorting/filtering changes the list as expected.

### Implementation for User Story 2

- [X] T020 [P] [US2] Add query/filter models in `ContosoDashboard/Models/DocumentQueryOptions.cs` and `ContosoDashboard/Models/DocumentFilterState.cs`
- [X] T021 [US2] Implement owner-scoped listing, sorting, and filtering in `ContosoDashboard/Services/DocumentService.cs`
- [X] T022 [P] [US2] Build reusable filtering and list UI in `ContosoDashboard/Shared/DocumentFilterBar.razor` and `ContosoDashboard/Shared/DocumentList.razor`
- [X] T023 [US2] Integrate the My Documents experience into `ContosoDashboard/Pages/Documents.razor`
- [ ] T024 [US2] Execute and record sorting/filter verification in `specs/1-document-management/quickstart.md`

**Checkpoint**: User Stories 1 and 2 both work independently

---

## Phase 5: User Story 3 - View and Download Project Documents (Priority: P3)

**Goal**: Let project members see project-linked documents and download only documents they are authorized to access.

**Independent Test**: Log in as a project member, open the project details page, verify associated documents are listed, and download one successfully; verify a non-member is denied.

### Implementation for User Story 3

- [X] T025 [US3] Extend project-scoped query and access rules in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/ProjectService.cs`
- [X] T026 [P] [US3] Implement the authorized file streaming endpoint in `ContosoDashboard/Pages/DocumentDownload.cshtml` and `ContosoDashboard/Pages/DocumentDownload.cshtml.cs`
- [X] T027 [P] [US3] Add the project documents section to `ContosoDashboard/Pages/ProjectDetails.razor`
- [X] T028 [US3] Wire project-context upload association from `ContosoDashboard/Pages/ProjectDetails.razor` into `ContosoDashboard/Shared/DocumentUploadForm.razor`
- [ ] T029 [US3] Execute and record project-visibility and denied-access checks in `specs/1-document-management/quickstart.md`

**Checkpoint**: User Stories 1-3 are independently functional

---

## Phase 6: User Story 4 - Search for Documents (Priority: P4)

**Goal**: Let users search accessible document metadata quickly without exposing inaccessible records.

**Independent Test**: Search for a keyword that matches an accessible document and verify it appears quickly; search for a keyword matching an inaccessible document and verify it is not returned.

### Implementation for User Story 4

- [X] T030 [US4] Implement metadata search, paging, and deterministic ordering in `ContosoDashboard/Services/DocumentService.cs`
- [X] T031 [P] [US4] Build the search input and empty-state UI in `ContosoDashboard/Shared/DocumentSearchBar.razor` and `ContosoDashboard/Shared/DocumentList.razor`
- [X] T032 [US4] Integrate search results and state handling into `ContosoDashboard/Pages/Documents.razor`
- [ ] T033 [US4] Execute and record special-character and latency checks in `specs/1-document-management/quickstart.md`

**Checkpoint**: Search is independently testable without relying on later stories

---

## Phase 7: User Story 5 - Manage Document Metadata and Delete (Priority: P5)

**Goal**: Let document owners update metadata, replace file content, and delete documents while preserving authorization and audit guarantees.

**Independent Test**: Edit a document title, replace the file, and delete the document as owner; verify a non-owner cannot perform the same actions.

### Implementation for User Story 5

- [X] T034 [P] [US5] Add metadata update and replace request models in `ContosoDashboard/Models/UpdateDocumentRequest.cs` and `ContosoDashboard/Models/ReplaceDocumentRequest.cs`
- [X] T035 [US5] Implement edit, replace, delete, and cleanup/audit behavior in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/DocumentActivityService.cs`
- [X] T036 [P] [US5] Build lifecycle action UI in `ContosoDashboard/Shared/DocumentEditModal.razor` and `ContosoDashboard/Shared/DocumentDeleteDialog.razor`
- [X] T037 [US5] Integrate lifecycle actions into `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Pages/ProjectDetails.razor`
- [ ] T038 [US5] Execute and record owner/manager lifecycle checks in `specs/1-document-management/quickstart.md`

**Checkpoint**: User Story 5 can be demonstrated without US6 or US7

---

## Phase 8: User Story 6 - Share Documents and Receive Notifications (Priority: P6)

**Goal**: Let document owners share documents with other users and surface those shares through notifications and a shared-documents view.

**Independent Test**: Share a document with another user, confirm the recipient receives a notification and can access the document from a Shared with Me view, and verify non-recipients are denied.

### Implementation for User Story 6

- [X] T039 [P] [US6] Add share request and shared-list models in `ContosoDashboard/Models/DocumentShareRequest.cs` and `ContosoDashboard/Models/SharedDocumentListItem.cs`
- [X] T040 [US6] Implement share persistence and recipient resolution in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/DocumentAuthorizationService.cs`
- [X] T041 [P] [US6] Integrate document share notifications in `ContosoDashboard/Services/NotificationService.cs` and `ContosoDashboard/Services/DocumentActivityService.cs`
- [X] T042 [P] [US6] Build sharing and Shared with Me UI in `ContosoDashboard/Shared/DocumentShareModal.razor` and `ContosoDashboard/Pages/Documents.razor`
- [X] T043 [US6] Add shared-documents navigation affordance in `ContosoDashboard/Shared/NavMenu.razor`
- [ ] T044 [US6] Execute and record sharing and access-denial checks in `specs/1-document-management/quickstart.md`

**Checkpoint**: User Story 6 adds collaboration without blocking prior story validation

---

## Phase 9: User Story 7 - Dashboard and Task Integration (Priority: P7)

**Goal**: Expose document summaries in the dashboard, attach documents to tasks, and provide administrator audit visibility.

**Independent Test**: Verify the dashboard shows recent documents and counts, a task shows/attaches related documents, and an administrator can open the activity log page.

### Implementation for User Story 7

- [X] T045 [P] [US7] Add dashboard document summary queries in `ContosoDashboard/Services/DashboardService.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [X] T046 [P] [US7] Add task attachment queries and commands in `ContosoDashboard/Services/TaskService.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [X] T047 [P] [US7] Add admin activity filtering/query support in `ContosoDashboard/Services/DocumentActivityService.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [X] T048 [US7] Update the dashboard UI in `ContosoDashboard/Pages/Index.razor`
- [X] T049 [US7] Update task document attachment UI in `ContosoDashboard/Pages/Tasks.razor`
- [X] T050 [US7] Create the admin audit page in `ContosoDashboard/Pages/DocumentActivity.razor`
- [X] T051 [US7] Update navigation for documents and admin audit access in `ContosoDashboard/Shared/NavMenu.razor`
- [X] T052 [US7] Execute and record dashboard/task/admin verification in `specs/1-document-management/quickstart.md`

**Checkpoint**: All user stories are independently functional

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improve user experience, performance, and cross-story verification.

- [X] T053 [P] Add PDF/image preview UI in `ContosoDashboard/Shared/DocumentPreview.razor` and integrate it into `ContosoDashboard/Pages/Documents.razor`
- [X] T054 Tune query performance and paging behavior in `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [ ] T055 Re-run and finalize the full end-to-end verification flow in `specs/1-document-management/quickstart.md`
- [X] T056 Review authorization failures, validation messages, and UX consistency in `ContosoDashboard/Pages/Documents.razor`, `ContosoDashboard/Pages/ProjectDetails.razor`, `ContosoDashboard/Pages/DocumentActivity.razor`, and `ContosoDashboard/Shared/DocumentList.razor`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: Depend on Foundational completion
- **Polish (Phase 10)**: Depends on the user stories you intend to ship

### User Story Dependencies

- **US1 (P1)**: Starts immediately after Phase 2; establishes the MVP
- **US2 (P2)**: Depends on US1 upload persistence being available
- **US3 (P3)**: Depends on foundational authorization/storage and benefits from US1 upload flow
- **US4 (P4)**: Depends on documents existing and searchable metadata from US1/US2
- **US5 (P5)**: Depends on US1 persistence and audit foundation
- **US6 (P6)**: Depends on US1 persistence plus notification plumbing and authorization foundation
- **US7 (P7)**: Depends on core document queries from US1-US6 and audit foundation

### Within Each User Story

- Models/view-models before service changes
- Services before UI integration
- Manual verification after the story UI is wired end-to-end
- Do not begin polish tasks until desired stories are stable

### Parallel Opportunities

- T003 can run in parallel with T001-T002 once the feature file map is agreed
- T005, T006, T010, and T011 can run in parallel after T004 starts the model layer
- Within each story, tasks marked `[P]` touch separate files and can proceed concurrently
- US5 and US6 can overlap after US1-US3 are stable if different people own service/UI slices

---

## Parallel Example: User Story 1

```text
T014 [US1] request/list models
T016 [US1] upload form component
```

## Parallel Example: User Story 2

```text
T020 [US2] query/filter models
T022 [US2] filter bar and list component
```

## Parallel Example: User Story 3

```text
T026 [US3] authorized download endpoint
T027 [US3] project documents UI section
```

## Parallel Example: User Story 4

```text
T031 [US4] search input and empty-state UI
T030 [US4] search service query
```

## Parallel Example: User Story 5

```text
T034 [US5] lifecycle request models
T036 [US5] edit/delete dialog components
```

## Parallel Example: User Story 6

```text
T041 [US6] notification integration
T042 [US6] sharing and Shared with Me UI
```

## Parallel Example: User Story 7

```text
T045 [US7] dashboard summary queries
T046 [US7] task attachment commands
T047 [US7] admin activity queries
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate upload, size/type rejection, and storage cleanup behavior
5. Demo the MVP before expanding scope

### Incremental Delivery

1. Finish Setup + Foundational once
2. Deliver US1 and validate it independently
3. Add US2-US3 for primary day-to-day access
4. Add US4-US6 for search and collaboration
5. Finish with US7 and Phase 10 polish

### Parallel Team Strategy

1. One developer handles persistence/storage foundation (T004-T013)
2. One developer handles main document workspace UI (`ContosoDashboard/Pages/Documents.razor` and shared components)
3. One developer handles project/task/dashboard integrations after the foundation lands

---

## Notes

- Total tasks: 56
- User story task counts: US1=6, US2=5, US3=5, US4=4, US5=5, US6=6, US7=8
- MVP scope: Phase 1 + Phase 2 + Phase 3 (US1)
- All tasks follow the required checklist format with IDs, optional `[P]`, required `[US#]` story labels in story phases, and explicit file paths