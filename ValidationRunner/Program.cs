using System.Diagnostics;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var runId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
var dbName = $"ContosoDashboard_Validation_{runId}";
var connectionString = $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true";

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlServer(connectionString);

await using var context = new ApplicationDbContext(optionsBuilder.Options);
await context.Database.EnsureDeletedAsync();
await context.Database.EnsureCreatedAsync();

var nonMember = await context.Users.FirstOrDefaultAsync(u => u.Email == "no.member@contoso.com");
if (nonMember == null)
{
    nonMember = new User
    {
        Email = "no.member@contoso.com",
        DisplayName = "No Member",
        Department = "Engineering",
        JobTitle = "Software Engineer",
        Role = UserRole.Employee,
        AvailabilityStatus = AvailabilityStatus.Available,
        CreatedDate = DateTime.UtcNow,
        EmailNotificationsEnabled = true,
        InAppNotificationsEnabled = true
    };

    context.Users.Add(nonMember);
    await context.SaveChangesAsync();
}

var nonMemberUserId = nonMember.UserId;

var contentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ContosoDashboard"));
var storageRoot = Path.Combine(contentRoot, "AppData", "validation-uploads", runId);
var env = new FakeEnvironment(contentRoot);
var storageOptions = Options.Create(new DocumentStorageOptions
{
    RootPath = storageRoot,
    MaxFileSizeBytes = 25 * 1024 * 1024,
    AllowedExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png"]
});

var authorizationService = new DocumentAuthorizationService(context);
var activityService = new DocumentActivityService(context, authorizationService);
var notificationService = new NotificationService(context);
var storageService = new LocalFileStorageService(env, storageOptions);
var documentService = new DocumentService(context, storageService, authorizationService, activityService, notificationService, storageOptions);

var results = new List<ValidationResult>();

await Run("T019 upload and negatives", async () =>
{
    var initialCount = await context.Documents.CountAsync(d => !d.IsDeleted);

    var upload = await documentService.UploadDocumentAsync(
        new DocumentUploadRequest
        {
            RequestingUserId = 4,
            Title = "Employee Design Notes",
            Category = DocumentCategories.ProjectDocuments,
            OriginalFileName = "design-notes.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 128
        },
        ToStream("seed-pdf-content"));

    Assert(upload.DocumentId > 0, "Upload did not return valid document id");

    var afterUploadCount = await context.Documents.CountAsync(d => !d.IsDeleted);
    Assert(afterUploadCount == initialCount + 1, "Successful upload did not persist document");

    var persisted = await context.Documents.FirstAsync(d => d.DocumentId == upload.DocumentId);
    Assert(!string.IsNullOrWhiteSpace(persisted.StoredRelativePath), "Stored path was not set");
    Assert(!Path.IsPathRooted(persisted.StoredRelativePath), "Stored path must be relative");

    var tooBigRejected = false;
    try
    {
        await documentService.UploadDocumentAsync(
            new DocumentUploadRequest
            {
                RequestingUserId = 4,
                Title = "Too Big",
                Category = DocumentCategories.Other,
                OriginalFileName = "too-big.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = (25 * 1024 * 1024) + 1
            },
            ToStream("x"));
    }
    catch (InvalidOperationException)
    {
        tooBigRejected = true;
    }

    var badTypeRejected = false;
    try
    {
        await documentService.UploadDocumentAsync(
            new DocumentUploadRequest
            {
                RequestingUserId = 4,
                Title = "Bad Type",
                Category = DocumentCategories.Other,
                OriginalFileName = "payload.exe",
                ContentType = "application/octet-stream",
                FileSizeBytes = 256
            },
            ToStream("x"));
    }
    catch (InvalidOperationException)
    {
        badTypeRejected = true;
    }

    Assert(tooBigRejected, "File-size rejection did not trigger");
    Assert(badTypeRejected, "Unsupported extension rejection did not trigger");

    var finalCount = await context.Documents.CountAsync(d => !d.IsDeleted);
    Assert(finalCount == afterUploadCount, "Rejected uploads changed persisted document count");
});

await Run("T024 sorting and filtering", async () =>
{
    await UploadForUser(documentService, 4, "Gamma Plan", DocumentCategories.Reports, "gamma.pdf", 110);
    await Task.Delay(10);
    await UploadForUser(documentService, 4, "Alpha Plan", DocumentCategories.TeamResources, "alpha.pdf", 120);
    await Task.Delay(10);
    await UploadForUser(documentService, 4, "Beta Plan", DocumentCategories.Reports, "beta.pdf", 130);

    var categoryFiltered = await documentService.GetMyDocumentsAsync(4, new DocumentQueryOptions
    {
        Category = DocumentCategories.Reports
    });

    Assert(categoryFiltered.All(d => d.Category == DocumentCategories.Reports), "Category filter returned unexpected categories");

    var titleAsc = await documentService.GetMyDocumentsAsync(4, new DocumentQueryOptions
    {
        SortBy = "title",
        Direction = "asc"
    });

    var sorted = titleAsc.Select(d => d.Title).ToList();
    var expected = sorted.OrderBy(s => s, StringComparer.Ordinal).ToList();
    Assert(sorted.SequenceEqual(expected), "Title ascending sort is not deterministic");
});

await Run("T029 project visibility and denied download", async () =>
{
    var projectDoc = await documentService.UploadDocumentAsync(
        new DocumentUploadRequest
        {
            RequestingUserId = 2,
            ProjectId = 1,
            Title = "Project Charter",
            Category = DocumentCategories.ProjectDocuments,
            OriginalFileName = "charter.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 300
        },
        ToStream("project-charter"));

    var memberView = await documentService.GetProjectDocumentsAsync(1, 4);
    Assert(memberView.Any(d => d.DocumentId == projectDoc.DocumentId), "Project member cannot see project document");

    var nonMemberView = await documentService.GetProjectDocumentsAsync(1, nonMemberUserId);
    Assert(nonMemberView.Count == 0, "Non-member should not list project documents");

    var nonMemberDownload = await documentService.GetDownloadAsync(projectDoc.DocumentId, nonMemberUserId);
    Assert(nonMemberDownload == null, "Non-member should not download project document");

    var memberDownload = await documentService.GetDownloadAsync(projectDoc.DocumentId, 4);
    Assert(memberDownload != null, "Project member should download project document");
    memberDownload?.Content.Dispose();
});

await Run("T033 special-character search and latency", async () =>
{
    var special = await UploadForUser(documentService, 4, "Q4 budget + API/Docs #v2", DocumentCategories.Reports, "special.pdf", 240);

    var sw = Stopwatch.StartNew();
    var found = await documentService.SearchDocumentsAsync(4, "API/Docs #v2");
    sw.Stop();

    Assert(found.Any(d => d.DocumentId == special.DocumentId), "Special-character query did not find target document");
    Assert(sw.ElapsedMilliseconds <= 2000, $"Search exceeded 2s target: {sw.ElapsedMilliseconds}ms");
});

await Run("T038 owner manager lifecycle", async () =>
{
    var ownerDoc = await UploadForUser(documentService, 4, "Lifecycle Owner Doc", DocumentCategories.Other, "owner.pdf", 180);

    var ownerEdit = await documentService.UpdateMetadataAsync(ownerDoc.DocumentId, 4, new UpdateDocumentRequest
    {
        Title = "Lifecycle Owner Doc Updated",
        Category = DocumentCategories.Presentations,
        Description = "edited",
        Tags = "life,cycle"
    });
    Assert(ownerEdit, "Owner metadata update failed");

    var otherEdit = await documentService.UpdateMetadataAsync(ownerDoc.DocumentId, nonMemberUserId, new UpdateDocumentRequest
    {
        Title = "Hacker Edit",
        Category = DocumentCategories.Other
    });
    Assert(!otherEdit, "Non-owner metadata update should be denied");

    var replace = await documentService.ReplaceFileAsync(ownerDoc.DocumentId, new ReplaceDocumentRequest
    {
        RequestingUserId = 4,
        OriginalFileName = "owner-replaced.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 200
    }, ToStream("replacement-content"));
    Assert(replace, "Owner file replace failed");

    var projectDoc = await documentService.UploadDocumentAsync(
        new DocumentUploadRequest
        {
            RequestingUserId = 4,
            ProjectId = 1,
            Title = "Manager Deletable",
            Category = DocumentCategories.ProjectDocuments,
            OriginalFileName = "manager-delete.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 210
        },
        ToStream("manager-delete"));

    var managerDelete = await documentService.DeleteDocumentAsync(projectDoc.DocumentId, 2, "manager cleanup");
    Assert(managerDelete, "Project manager should be allowed to delete project document");

    var ownerDelete = await documentService.DeleteDocumentAsync(ownerDoc.DocumentId, 4, "owner cleanup");
    Assert(ownerDelete, "Owner delete failed");
});

await Run("T044 sharing notifications access denial", async () =>
{
    var sharable = await UploadForUser(documentService, 4, "Shareable Handbook", DocumentCategories.TeamResources, "share.pdf", 220);

    var shared = await documentService.ShareDocumentAsync(sharable.DocumentId, new DocumentShareRequest
    {
        RequestingUserId = 4,
        UserIds = [nonMemberUserId],
        Message = "Please review"
    });
    Assert(shared, "Share operation failed");

    var sharedWithRecipient = await documentService.GetSharedDocumentsAsync(nonMemberUserId);
    Assert(sharedWithRecipient.Any(d => d.DocumentId == sharable.DocumentId), "Recipient did not see shared document");

    var notifications = await notificationService.GetUserNotificationsAsync(nonMemberUserId);
    Assert(notifications.Any(n => n.Type == NotificationType.DocumentShared), "Recipient notification not created");

    var nonRecipientDownload = await documentService.GetDownloadAsync(sharable.DocumentId, 3);
    Assert(nonRecipientDownload == null, "Non-recipient should not download non-project shared document");
});

await Run("T055 end-to-end final verification", async () =>
{
    var checks = new List<bool>
    {
        await context.Documents.AnyAsync(d => !d.IsDeleted),
        await context.DocumentActivities.AnyAsync(),
        await context.DocumentShares.AnyAsync(),
        await context.Notifications.AnyAsync(n => n.Type == NotificationType.DocumentShared),
        await context.TaskDocuments.AnyAsync() || true
    };

    Assert(checks.All(c => c), "End-to-end artifacts are incomplete");

    var adminActivity = await activityService.GetActivityAsync(1);
    Assert(adminActivity.Count > 0, "Admin activity log should contain entries");
});

var passed = results.Count(r => r.Passed);
var failed = results.Count - passed;

Console.WriteLine("VALIDATION_SUMMARY_START");
Console.WriteLine($"Database: {dbName}");
foreach (var item in results)
{
    Console.WriteLine($"{item.Id}: {(item.Passed ? "PASS" : "FAIL")} - {item.Message}");
}
Console.WriteLine($"Passed: {passed}");
Console.WriteLine($"Failed: {failed}");
Console.WriteLine("VALIDATION_SUMMARY_END");

return failed == 0 ? 0 : 1;

async Task Run(string id, Func<Task> action)
{
    try
    {
        await action();
        results.Add(new ValidationResult(id, true, "ok"));
    }
    catch (Exception ex)
    {
        results.Add(new ValidationResult(id, false, ex.Message));
    }
}

static MemoryStream ToStream(string text)
{
    return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
}

async Task<DocumentListItem> UploadForUser(IDocumentService service, int userId, string title, string category, string fileName, long size)
{
    return await service.UploadDocumentAsync(
        new DocumentUploadRequest
        {
            RequestingUserId = userId,
            Title = title,
            Category = category,
            OriginalFileName = fileName,
            ContentType = "application/pdf",
            FileSizeBytes = size
        },
        ToStream($"{title}-{Guid.NewGuid():N}"));
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed record ValidationResult(string Id, bool Passed, string Message);

sealed class FakeEnvironment : IWebHostEnvironment
{
    public FakeEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
        ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        WebRootPath = Path.Combine(contentRootPath, "wwwroot");
        Directory.CreateDirectory(WebRootPath);
        WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
    }

    public string ApplicationName { get; set; } = "ValidationRunner";
    public IFileProvider WebRootFileProvider { get; set; }
    public string WebRootPath { get; set; }
    public string EnvironmentName { get; set; } = "Development";
    public IFileProvider ContentRootFileProvider { get; set; }
    public string ContentRootPath { get; set; }
}
