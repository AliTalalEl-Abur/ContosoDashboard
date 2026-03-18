# Feature Specification: Document Upload and Management

**Feature Branch**: `1-document-management`  
**Created**: 2026-03-18  
**Status**: Draft  
**Input**: StakeholderDocs/document-upload-and-management-feature.md

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Upload a Document (Priority: P1)

An employee selects one or more files from their computer, provides a title and category,
optionally links the document to a project, and submits the upload. The system stores
the file securely, saves the metadata, and confirms success. This is the core action
everything else depends on.

**Why this priority**: Without the ability to upload documents, no other story is
meaningful. This is the entry point for all document-management value.

**Independent Test**: Log in as any employee, navigate to the Documents page, upload a
PDF under 25 MB with a title and a category. Verify the document appears in "My
Documents" and its metadata matches what was entered.

**Acceptance Scenarios**:

1. **Given** a logged-in Employee with no prior uploads, **When** they select a valid
   PDF (< 25 MB), enter a title and category, and submit, **Then** the document is listed
   in "My Documents" with correct title, category, upload date, file size, and their
   name as uploader.
2. **Given** a logged-in Employee, **When** they select a file exceeding 25 MB,
   **Then** the system rejects the upload and displays a clear error message before any
   storage occurs.
3. **Given** a logged-in Employee, **When** they select an unsupported file type (e.g.,
   `.exe`), **Then** the system rejects the upload with an error message listing accepted
   types.
4. **Given** a logged-in Employee, **When** they omit the required title or category,
   **Then** the form prevents submission and highlights the missing fields.

---

### User Story 2 - Browse and Filter My Documents (Priority: P2)

An employee views a personal document library showing all documents they have uploaded.
They can sort the list by title, upload date, category, or file size, and filter by
category, associated project, or date range to narrow results.

**Why this priority**: After uploading, users need to find their own documents quickly.
This is the primary day-to-day interaction for most employees.

**Independent Test**: Log in as any employee who has uploaded at least three documents
across two categories. Verify the list shows all their documents, that sorting by
upload date changes the order, and that filtering by one category hides documents from
other categories.

**Acceptance Scenarios**:

1. **Given** an employee with uploaded documents, **When** they open "My Documents",
   **Then** only their documents are visible (other users' documents are never shown).
2. **Given** the document list, **When** the employee sorts by upload date descending,
   **Then** the most recently uploaded document appears first.
3. **Given** the document list, **When** the employee filters by category
   "Reports", **Then** only documents in that category are shown.
4. **Given** the document list, **When** the employee filters by a date range,
   **Then** only documents uploaded within that range appear.

---

### User Story 3 - View and Download Project Documents (Priority: P3)

Any team member on a project can view all documents associated with that project from
the project detail page. They can download any document they have permission to access.

**Why this priority**: The project-level view closes the loop between documents and the
work they support. It is the primary collaboration touchpoint for project teams.

**Independent Test**: Log in as a team member on a project that has at least one
document. Open the project detail page and verify the documents tab/section is present.
Download a document and confirm the correct file is received.

**Acceptance Scenarios**:

1. **Given** a project with associated documents, **When** a project team member opens
   the project detail page, **Then** all documents associated with that project are
   listed with title, category, upload date, and uploader.
2. **Given** a project with documents, **When** a user who is NOT a team member on that
   project navigates to the project detail URL directly, **Then** they are denied access
   to both the project and its documents.
3. **Given** a project team member viewing project documents, **When** they click
   Download on a document, **Then** the correct file is served and saved to their device.
4. **Given** a Project Manager, **When** they upload a document directly from the
   project detail page, **Then** the document is automatically associated with that
   project and visible to all team members.

---

### User Story 4 - Search for Documents (Priority: P4)

Users can search for documents across their accessible document space using keywords
that match title, description, tags, uploader name, or associated project. Results
return within 2 seconds and respect access rules.

**Why this priority**: Search becomes valuable once users accumulate many documents.
It is important but less urgent than the foundational upload, browse, and project-view
flows.

**Independent Test**: Log in as an employee, upload two documents with distinct titles
and tags, then search for a keyword that appears in only one document. Verify only the
matching document appears in results.

**Acceptance Scenarios**:

1. **Given** a search query matching a document title the user uploaded, **When** the
   user submits the search, **Then** the matching document appears in results within
   2 seconds.
2. **Given** a search query matching a document the user does NOT have access to,
   **When** the search runs, **Then** that document does not appear in results.
3. **Given** a search with no matching documents, **When** submitted, **Then** an empty
   state message is shown (no errors).

---

### User Story 5 - Manage Document Metadata and Delete (Priority: P5)

Document owners can edit a document's title, description, category, and tags after
upload, replace the file with an updated version, and permanently delete documents they
own. Project Managers can delete any document in their projects.

**Why this priority**: Editing and deletion are important lifecycle actions but do not
block earlier stories and are lower risk to defer.

**Independent Test**: Log in as the uploader of a document. Edit its title and save.
Verify the updated title appears in "My Documents". Then delete the document and verify
it no longer appears in any list.

**Acceptance Scenarios**:

1. **Given** a document the user uploaded, **When** they edit the title and save,
   **Then** the updated title is immediately reflected in the document list.
2. **Given** a document uploaded by another user, **When** a non-owner employee tries
   to edit or delete it, **Then** the edit/delete actions are not available to them.
3. **Given** a Project Manager viewing project documents, **When** they delete a
   document uploaded by a team member, **Then** the document is permanently removed and
   the team member can no longer access it.
4. **Given** an owner of a document, **When** they confirm deletion, **Then** the file
   and its metadata are permanently removed with no recovery option.

---

### User Story 6 - Share Documents and Receive Notifications (Priority: P6)

Document owners can share a document with specific users or teams. Recipients receive
an in-app notification and can find the shared document in a "Shared with Me" section.

**Why this priority**: Sharing is a collaboration enhancement that builds on the core
document store. It requires stable upload, browse, and notification infrastructure
first.

**Independent Test**: Log in as a document owner. Share a document with a specific
other user. Log in as that user and verify an in-app notification has arrived and the
document appears under "Shared with Me".

**Acceptance Scenarios**:

1. **Given** a document owner, **When** they share a document with a colleague,
   **Then** the colleague receives an in-app notification linking to the document.
2. **Given** a recipient of a shared document, **When** they open "Shared with Me",
   **Then** the shared document is listed and downloadable.
3. **Given** a user who has not been explicitly shared a document and is not on the
   owning project, **When** they navigate to the document URL directly, **Then** they
   are denied access.

---

### User Story 7 - Dashboard and Task Integration (Priority: P7)

The dashboard home page shows a "Recent Documents" widget listing the user's last 5
uploaded documents. From any task detail view, users can see and attach related
documents. The dashboard summary cards include a document count.

**Why this priority**: These are integration enhancements that improve discoverability.
They depend on all core document functionality being stable.

**Independent Test**: Log in as a user who has uploaded at least one document. Open the
dashboard and verify the "Recent Documents" widget displays their most recently
uploaded document with a link.

**Acceptance Scenarios**:

1. **Given** a user with recent uploads, **When** they open the dashboard, **Then** the
   "Recent Documents" widget shows up to 5 of their most recently uploaded documents.
2. **Given** a task associated with a project, **When** a project team member views the
   task detail, **Then** documents linked to that task are visible and downloadable.
3. **Given** a user on a task detail page, **When** they attach a document to the task,
   **Then** the document is automatically associated with the task's parent project.

---

### Edge Cases

- An authenticated user attempts to download or edit a document from a project they
  were removed from after the document was uploaded: access MUST be denied at the
  service layer, not just the UI.
- A file save succeeds but the database insert fails: the orphaned file MUST be cleaned
  up or the operation MUST roll back so no metadata record exists without a backing
  file.
- A user uploads a file, the upload completes, but the user immediately navigates away:
  the document MUST still be persisted and appear when the user returns.
- Two users simultaneously upload documents with identical original filenames to the
  same project: both MUST succeed and produce distinct stored files with unique paths.
- A search query contains special characters (quotes, percent signs, angle brackets):
  the system MUST handle these without errors or data leakage.
- A user attempts to preview a file type not supported for preview (e.g., `.xlsx`):
  the system falls back gracefully to a download prompt.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to upload files of types PDF, DOC, DOCX, XLS, XLSX,
  PPT, PPTX, TXT, JPG, JPEG, and PNG.
- **FR-002**: System MUST reject uploaded files exceeding 25 MB and display a clear
  error message identifying the size limit.
- **FR-003**: System MUST reject files with unsupported extensions and display the list
  of accepted types.
- **FR-004**: Users MUST provide a document title (required) and category (required from
  a predefined list: Project Documents, Team Resources, Personal Files, Reports,
  Presentations, Other) before an upload is accepted.
- **FR-005**: Users MAY optionally provide a description, an associated project, and
  free-form tags when uploading.
- **FR-006**: System MUST automatically capture upload date and time, uploader identity,
  file size, and file MIME type on every upload.
- **FR-007**: System MUST store uploaded files outside the web-accessible directory
  using a GUID-based path pattern to prevent path traversal and direct URL access.
- **FR-008**: System MUST save the file to disk before committing document metadata to
  the database to prevent orphaned records.
- **FR-009**: Users MUST be able to view a "My Documents" list showing all documents
  they have uploaded, sortable by title, upload date, category, and file size.
- **FR-010**: Users MUST be able to filter the "My Documents" list by category,
  associated project, and date range.
- **FR-011**: Project team members MUST be able to view and download all documents
  associated with projects they belong to from the project detail page.
- **FR-012**: Users MUST be able to search for documents by title, description, tags,
  uploader name, and associated project; results MUST be returned within 2 seconds.
- **FR-013**: Document owners MUST be able to edit the title, description, category,
  and tags of their documents.
- **FR-014**: Document owners MUST be able to replace the file content of a document
  while preserving its metadata record.
- **FR-015**: Document owners MUST be able to permanently delete documents they
  uploaded, with a confirmation step before deletion.
- **FR-016**: Project Managers MUST be able to delete any document associated with
  their projects.
- **FR-017**: Document owners MUST be able to share documents with specific users or
  teams; recipients MUST receive an in-app notification.
- **FR-018**: Shared documents MUST appear in a "Shared with Me" section for recipients.
- **FR-019**: System MUST log all document-related activities: upload, download,
  deletion, and share events, accessible to Administrators.
- **FR-020**: The dashboard home page MUST display a "Recent Documents" widget showing
  the logged-in user's 5 most recently uploaded documents.
- **FR-021**: Dashboard summary cards MUST include a document count for the current user.
- **FR-022**: Task detail views MUST allow users to view and attach documents; attached
  documents are automatically associated with the task's parent project.
- **FR-023**: For PDF and image files, the system SHOULD offer an in-browser preview
  before requiring download.

### Security and Access Rules *(mandatory)*

| Role              | Upload | View Own | View Project Docs | Edit Own | Delete Own | Delete Any in Project | View Admin Reports |
|-------------------|--------|----------|-------------------|----------|------------|-----------------------|--------------------|
| Employee          | ✅     | ✅       | Project members only | ✅    | ✅         | ❌                    | ❌                 |
| Team Lead         | ✅     | ✅       | Project members only | ✅    | ✅         | ❌ (own project only via PM role) | ❌     |
| Project Manager   | ✅     | ✅       | All their projects | ✅     | ✅         | ✅ (own projects)     | ❌                 |
| Administrator     | ✅     | ✅       | All              | ✅       | ✅         | ✅ (all)              | ✅                 |

- The document download endpoint MUST verify at the service layer that the requesting
  user is the uploader, a member of the associated project, a recipient of a share, or
  an Administrator — any other access MUST return a 403 response.
- Document identifiers in URLs MUST be resolved through service-layer authorization
  checks to prevent Insecure Direct Object Reference (IDOR) attacks.
- File names stored on disk MUST be GUID-based; the original user-supplied filename
  MUST only be stored as display metadata, never used to construct file system paths.
- File extension MUST be validated against an explicit allowlist server-side, regardless
  of client-side validation.
- All document activity events (uploads, downloads, deletions, shares) MUST be recorded
  for Administrator audit access.

### Operational Constraints & Migration Notes *(mandatory)*

- **Offline-first**: The feature MUST function completely without cloud connectivity,
  storing files in a local directory (e.g., `AppData/uploads/`) outside `wwwroot`.
- **Interface abstraction**: All file storage operations MUST be routed through an
  `IFileStorageService` interface. The training implementation MUST use
  `LocalFileStorageService` backed by `System.IO`. A future `AzureBlobStorageService`
  can replace it via dependency injection with no business logic changes.
- **File path portability**: Paths stored in the database MUST be relative
  (e.g., `{userId}/{projectId}/{guid}.{ext}`) so the root storage location can be
  reconfigured without a data migration.
- **Mock authentication compatibility**: The feature MUST use the existing claims-based
  identity model. All user identification MUST read from the current user's claims
  (NameIdentifier, Role, Department); no separate login mechanism is introduced.
- **Non-goals for this training implementation**: virus/malware scanning, CDN delivery,
  versioning history UI, and cloud storage are explicitly deferred. These are noted as
  production follow-up items.
- **Database keys**: `DocumentId` MUST use integer keys for consistency with existing
  tables. Category MUST store text values (not integer enums).

### Key Entities *(mandatory for this feature)*

- **Document**: Represents a single uploaded file and its metadata. Key attributes:
  integer ID, title, optional description, category (text), original filename, internal
  GUID-based file path, MIME type, file size in bytes, upload timestamp, uploader
  (User foreign key), optional project association (Project foreign key).
- **DocumentShare**: Represents a sharing relationship. Key attributes: document
  reference, recipient user or team reference, share timestamp, shared-by user
  reference.
- **DocumentActivity**: Represents an audit log entry. Key attributes: document
  reference, action type (upload/download/delete/share), actor user, timestamp.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can locate a document by title using search in under 30 seconds
  from opening the application.
- **SC-002**: A single file up to 25 MB completes the full upload-and-confirm flow in
  under 30 seconds on a local machine.
- **SC-003**: The "My Documents" list for up to 500 documents loads within 2 seconds.
- **SC-004**: Document search returns results within 2 seconds for up to 500 accessible
  documents.
- **SC-005**: Unauthorized access attempts to documents (wrong user, removed project
  member, direct URL) are rejected 100% of the time with no data leakage.
- **SC-006**: Within 3 months of launch, 70% of active dashboard users have uploaded at
  least one document.
- **SC-007**: 90% of uploaded documents are properly categorized (non-"Other" category
  selected).
- **SC-008**: Zero security incidents related to unauthorized document access.

---

## Verification Evidence *(mandatory)*

- **Automated Verification**: No automated test project exists yet. Given the Blazor
  Server file-upload workflow and the training scope, automated tests are deferred.
  Candidate test targets for a future test project: `DocumentService` unit tests for
  size/extension validation logic and IDOR authorization checks; integration tests for
  the upload-then-retrieve happy path.
- **Manual Verification**:
  1. Log in as Employee (e.g., Ni Kang). Upload a PDF under 25 MB with title "Test Doc"
     and category "Reports". Verify the document appears in "My Documents" with correct
     metadata.
  2. Attempt to upload a file > 25 MB. Verify a clear error is shown and nothing is
     stored.
  3. Attempt to upload a `.exe` file. Verify rejection with accepted-types message.
  4. Log in as a second employee and attempt to access the first user's document URL
     directly. Verify 403/redirect with no content served.
  5. Log in as a Project Manager. Upload a document to one of their projects. Log in as
     a team member on that project and verify the document appears in the project detail
     view and can be downloaded.
  6. Log in as a team member NOT on the project. Verify the project document is not
     accessible.
  7. Share a document with a colleague. Log in as that colleague and verify the in-app
     notification and the "Shared with Me" section show the document.
  8. Delete a document as its owner. Confirm it no longer appears in any list and the
     physical file is removed from storage.
  9. Open the dashboard and verify "Recent Documents" shows the last 5 uploads.

---

## Assumptions

- The existing `[Authorize]` attribute and cookie-based authentication middleware remain
  unchanged; this feature adds no new login mechanism.
- "Teams" in the share flow refers to project membership groups already modeled in the
  database, not a separate team entity.
- PDF preview is implemented via the browser's native PDF viewer (`<embed>` or
  `<iframe>` with a blob/object URL returned by the authorized download endpoint).
- Image preview uses a similar authorized-endpoint pattern with an `<img>` tag.
- The "Reports" section for Administrators is a simple filtered activity log view, not
  a dedicated reporting engine.
- File replace (FR-014) updates the physical file and internal path while retaining the
  same Document record ID and all user-set metadata.
