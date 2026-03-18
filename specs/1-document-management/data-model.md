# Data Model: Document Upload and Management

## Entity: Document

Purpose: Represents one uploaded file and its metadata.

Fields:
- `Id` (int, PK)
- `Title` (nvarchar(200), required)
- `Description` (nvarchar(2000), nullable)
- `Category` (nvarchar(64), required)
- `Tags` (nvarchar(500), nullable, delimited string for v1)
- `OriginalFileName` (nvarchar(255), required)
- `StoredRelativePath` (nvarchar(400), required)
- `MimeType` (nvarchar(127), required)
- `FileSizeBytes` (bigint, required, <= 25 MB enforced in service)
- `UploadedAtUtc` (datetime2, required)
- `UploadedByUserId` (int, FK -> User, required)
- `ProjectId` (int, FK -> Project, nullable)
- `TaskId` (int, FK -> TaskItem, nullable)
- `IsDeleted` (bit, required, default false)
- `DeletedAtUtc` (datetime2, nullable)
- `DeletedByUserId` (int, FK -> User, nullable)

Indexes:
- `IX_Document_UploadedByUserId_UploadedAtUtc`
- `IX_Document_ProjectId_UploadedAtUtc`
- `IX_Document_Title`
- `IX_Document_Category`

Validation rules:
- `Title` required and trimmed.
- `Category` must be one of predefined values.
- Extension must be allowlisted.
- `FileSizeBytes` <= 25 MB.
- `StoredRelativePath` must not be absolute.

State transitions:
- `Active` -> `Deleted` when owner/PM/Admin confirms delete.
- Replace file keeps same `Id` and metadata identity, updates storage path + MIME + size.

## Entity: DocumentShare

Purpose: Grants access to a document by explicit recipient identity.

Fields:
- `Id` (int, PK)
- `DocumentId` (int, FK -> Document, required)
- `SharedByUserId` (int, FK -> User, required)
- `SharedWithUserId` (int, FK -> User, nullable)
- `SharedWithProjectId` (int, FK -> Project, nullable)
- `SharedAtUtc` (datetime2, required)
- `Message` (nvarchar(500), nullable)

Validation rules:
- At least one recipient target is set (`SharedWithUserId` or `SharedWithProjectId`).
- Duplicate active share rows for same target should be prevented.

Indexes:
- `IX_DocumentShare_DocumentId`
- `IX_DocumentShare_SharedWithUserId`
- `IX_DocumentShare_SharedWithProjectId`

## Entity: DocumentActivity

Purpose: Immutable audit trail for document events.

Fields:
- `Id` (int, PK)
- `DocumentId` (int, FK -> Document, required)
- `ActorUserId` (int, FK -> User, required)
- `ActionType` (nvarchar(32), required) // Upload, Download, Share, UpdateMetadata, ReplaceFile, Delete
- `OccurredAtUtc` (datetime2, required)
- `DetailsJson` (nvarchar(max), nullable)

Indexes:
- `IX_DocumentActivity_DocumentId_OccurredAtUtc`
- `IX_DocumentActivity_ActorUserId_OccurredAtUtc`
- `IX_DocumentActivity_ActionType_OccurredAtUtc`

## Entity: TaskDocument (optional bridge)

Purpose: Normalizes many-to-many task attachment mapping if task linking expands.

Fields:
- `TaskId` (int, FK -> TaskItem, PK part)
- `DocumentId` (int, FK -> Document, PK part)
- `AttachedAtUtc` (datetime2, required)
- `AttachedByUserId` (int, FK -> User, required)

Note:
- If v1 keeps single nullable `TaskId` on `Document`, this bridge can be deferred.

## Relationships

- `User (1) -> (many) Document` via `UploadedByUserId`
- `Project (1) -> (many) Document` via nullable `ProjectId`
- `Document (1) -> (many) DocumentShare`
- `Document (1) -> (many) DocumentActivity`
- `User (1) -> (many) DocumentShare` as sharer and recipient
- `Project (1) -> (many) DocumentShare` for project-target shares
- `TaskItem (many) <-> (many) Document` via `TaskDocument` (or nullable FK simplification)

## Authorization matrix implications

- View/download allowed if actor is:
  - document owner,
  - project member for associated project,
  - explicit share recipient,
  - Administrator.
- Edit/replace/delete allowed for owner; PM/Admin elevated delete by project/global scope.

## Migration notes

- Use integer PKs for all new entities.
- Keep category as text, not enum-backed integer.
- Backfill not required (new feature tables).
