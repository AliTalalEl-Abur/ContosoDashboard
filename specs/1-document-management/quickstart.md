# Quickstart: Document Upload and Management

This guide validates the feature manually in the local training environment.

## Prerequisites

- .NET SDK compatible with project target.
- LocalDB available.
- Application runs locally from `ContosoDashboard/`.
- Test users exist for Employee, Project Manager, and Administrator roles.

## Run application

1. From repository root:
   - `cd ContosoDashboard`
   - `dotnet restore`
   - `dotnet run`
2. Open the local URL shown in terminal.

## Validate core flow

1. Login as Employee.
2. Open Documents page.
3. Upload valid PDF (<25 MB) with required title/category.
4. Verify document appears in My Documents with metadata.

## Validate negative paths

1. Try uploading file >25 MB.
2. Try uploading unsupported extension (`.exe`).
3. Verify clear validation errors and no persisted document.

## Validate authorization

1. As User A, upload a project document.
2. As User B (not project member), attempt direct download URL.
3. Verify 403/denied behavior and no file content leak.

## Validate project and sharing behavior

1. As Project Manager, upload document from project detail page.
2. As project member, verify visibility and download.
3. Share a document with another user.
4. As recipient, verify notification + Shared with Me entry.

## Validate lifecycle and audit

1. Edit metadata and verify list reflects updates.
2. Replace file content and verify same document identity remains.
3. Delete document as owner and verify metadata/file are removed.
4. Login as Administrator and verify activity log shows upload/download/share/delete events.

## Performance checks

- Upload path should complete <= 30 seconds for <= 25 MB file.
- My Documents and search should return <= 2 seconds at typical local dataset scale.

## Notes

- Files must be stored outside `wwwroot`.
- Stored path should be GUID-based and relative.
- If DB write fails after file save, cleanup logic should remove orphan file.

## Execution Log (2026-03-18)

Environment
- App URL: `http://localhost:5055`
- Connection string override: `ContosoDashboard_DocValidation`
- Seed users used: Admin (`UserId=1`), Employee (`UserId=4`)

Validated checks (session + cookie flow via `/login`)
- Dashboard renders document summary card (`Documents`) and `Recent Documents` section for authenticated employee.
- Tasks page loads for authenticated employee and renders attachment action (`bi bi-paperclip`) indicating task-document integration entry point.
- Admin can open `/documents/activity` and sees `Document Activity` heading plus filters (`Action`, `Actor User ID`, `Document ID`, `Apply`).
- Employee access to `/documents/activity` is denied (authorization boundary enforced; no admin heading rendered).

Validation output snapshot
```json
{
   "DashboardDocumentsCard": true,
   "DashboardRecentDocuments": true,
   "TasksRouteLoads": true,
   "TasksHasAttachmentAction": true,
   "AdminActivityLoads": true,
   "AdminActivityHasFilters": true,
   "EmployeeDeniedFromAdminActivity": true,
   "CheckedAtUtc": "2026-03-18 22:14:39Z"
}
```

Scope note
- This run covers US7 integration visibility and authorization/UX consistency checks needed for T052 and T056.
- Full end-to-end re-run for all stories (including upload/share/lifecycle interaction sequence) remains tracked by T055.
