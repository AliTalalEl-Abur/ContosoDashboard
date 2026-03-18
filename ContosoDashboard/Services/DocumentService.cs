using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<DocumentListItem> UploadDocumentAsync(DocumentUploadRequest request, Stream content, CancellationToken cancellationToken = default);
    Task<List<DocumentListItem>> GetMyDocumentsAsync(int userId, DocumentQueryOptions? options = null);
    Task<List<DocumentListItem>> GetSharedDocumentsAsync(int userId);
    Task<List<DocumentListItem>> GetProjectDocumentsAsync(int projectId, int userId);
    Task<List<DocumentListItem>> SearchDocumentsAsync(int userId, string searchText);
    Task<DocumentDownloadResult?> GetDownloadAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(int documentId, int userId, UpdateDocumentRequest request);
    Task<bool> ReplaceFileAsync(int documentId, ReplaceDocumentRequest request, Stream content, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(int documentId, int userId, string? reason = null);
    Task<bool> ShareDocumentAsync(int documentId, DocumentShareRequest request);
    Task<List<DocumentListItem>> GetTaskDocumentsAsync(int taskId, int userId);
    Task<bool> AttachDocumentToTaskAsync(int taskId, int documentId, int userId);
    Task<List<DocumentListItem>> GetRecentDocumentsAsync(int userId, int take = 5);
    Task<int> GetDocumentCountAsync(int userId);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDocumentAuthorizationService _authorizationService;
    private readonly IDocumentActivityService _activityService;
    private readonly INotificationService _notificationService;
    private readonly DocumentStorageOptions _storageOptions;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IDocumentAuthorizationService authorizationService,
        IDocumentActivityService activityService,
        INotificationService notificationService,
        IOptions<DocumentStorageOptions> storageOptions)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _authorizationService = authorizationService;
        _activityService = activityService;
        _notificationService = notificationService;
        _storageOptions = storageOptions.Value;
    }

    public async Task<DocumentListItem> UploadDocumentAsync(DocumentUploadRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        ValidateUploadRequest(request);

        if (request.TaskId.HasValue)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .ThenInclude(p => p.ProjectMembers)
                .FirstOrDefaultAsync(t => t.TaskId == request.TaskId.Value, cancellationToken);

            if (task == null)
            {
                throw new InvalidOperationException("The selected task does not exist.");
            }

            var hasTaskAccess = task.AssignedUserId == request.RequestingUserId
                || task.CreatedByUserId == request.RequestingUserId
                || task.Project?.ProjectManagerId == request.RequestingUserId
                || task.Project?.ProjectMembers.Any(pm => pm.UserId == request.RequestingUserId) == true;

            if (!hasTaskAccess)
            {
                throw new InvalidOperationException("You are not authorized to attach documents to this task.");
            }

            if (!request.ProjectId.HasValue && task.ProjectId.HasValue)
            {
                request.ProjectId = task.ProjectId;
            }
        }

        if (request.ProjectId.HasValue && !await _authorizationService.CanAccessProjectAsync(request.ProjectId.Value, request.RequestingUserId))
        {
            throw new InvalidOperationException("You are not authorized to upload documents to this project.");
        }

        FileStorageSaveResult? savedFile = null;

        try
        {
            savedFile = await _fileStorageService.SaveAsync(request.RequestingUserId, request.ProjectId, request.OriginalFileName, content, cancellationToken);

            var document = new Document
            {
                Title = request.Title.Trim(),
                Description = NormalizeNullable(request.Description),
                Category = request.Category,
                Tags = NormalizeTags(request.Tags),
                OriginalFileName = request.OriginalFileName,
                StoredRelativePath = savedFile.RelativePath,
                MimeType = NormalizeContentType(request.ContentType),
                FileSizeBytes = request.FileSizeBytes,
                UploadedAtUtc = DateTime.UtcNow,
                UploadedByUserId = request.RequestingUserId,
                ProjectId = request.ProjectId,
                IsDeleted = false
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);

            if (request.TaskId.HasValue)
            {
                _context.TaskDocuments.Add(new TaskDocument
                {
                    TaskId = request.TaskId.Value,
                    DocumentId = document.DocumentId,
                    AttachedAtUtc = DateTime.UtcNow,
                    AttachedByUserId = request.RequestingUserId
                });

                await _context.SaveChangesAsync(cancellationToken);
            }

            await _activityService.LogAsync(document.DocumentId, request.RequestingUserId, DocumentActivityTypes.Upload);

            return await BuildListItemAsync(document.DocumentId, request.RequestingUserId, cancellationToken)
                ?? throw new InvalidOperationException("Uploaded document could not be loaded.");
        }
        catch
        {
            if (savedFile != null)
            {
                await _fileStorageService.DeleteAsync(savedFile.RelativePath, cancellationToken);
            }

            throw;
        }
    }

    public async Task<List<DocumentListItem>> GetMyDocumentsAsync(int userId, DocumentQueryOptions? options = null)
    {
        var query = _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Where(d => !d.IsDeleted && d.UploadedByUserId == userId);

        query = ApplyOptions(query, options);
        var documents = await query.ToListAsync();

        return await MapDocumentsAsync(documents, userId);
    }

    public async Task<List<DocumentListItem>> GetSharedDocumentsAsync(int userId)
    {
        var documentIds = await _context.DocumentShares
            .Where(ds => ds.SharedWithUserId == userId ||
                (ds.SharedWithProjectId.HasValue && _context.ProjectMembers.Any(pm => pm.ProjectId == ds.SharedWithProjectId && pm.UserId == userId)))
            .Select(ds => ds.DocumentId)
            .Distinct()
            .ToListAsync();

        var documents = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Include(d => d.Shares)
            .Where(d => !d.IsDeleted && documentIds.Contains(d.DocumentId) && d.UploadedByUserId != userId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();

        return await MapDocumentsAsync(documents, userId);
    }

    public async Task<List<DocumentListItem>> GetProjectDocumentsAsync(int projectId, int userId)
    {
        if (!await _authorizationService.CanAccessProjectAsync(projectId, userId) && !await _authorizationService.IsAdministratorAsync(userId))
        {
            return new List<DocumentListItem>();
        }

        var documents = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Where(d => !d.IsDeleted && d.ProjectId == projectId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();

        return await MapDocumentsAsync(documents, userId);
    }

    public async Task<List<DocumentListItem>> SearchDocumentsAsync(int userId, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new List<DocumentListItem>();
        }

        var normalized = searchText.Trim().ToLowerInvariant();
        var query = GetAccessibleDocumentsQuery(userId)
            .Where(d =>
                d.Title.ToLower().Contains(normalized)
                || (d.Description != null && d.Description.ToLower().Contains(normalized))
                || (d.Tags != null && d.Tags.ToLower().Contains(normalized))
                || d.UploadedByUser.DisplayName.ToLower().Contains(normalized)
                || (d.Project != null && d.Project.Name.ToLower().Contains(normalized)))
            .OrderByDescending(d => d.UploadedAtUtc)
            .Take(100);

        var documents = await query.ToListAsync();
        return await MapDocumentsAsync(documents, userId);
    }

    public async Task<List<DocumentListItem>> GetTaskDocumentsAsync(int taskId, int userId)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .ThenInclude(p => p.ProjectMembers)
            .FirstOrDefaultAsync(t => t.TaskId == taskId);

        if (task == null)
        {
            return new List<DocumentListItem>();
        }

        var hasTaskAccess = task.AssignedUserId == userId
            || task.CreatedByUserId == userId
            || task.Project?.ProjectManagerId == userId
            || task.Project?.ProjectMembers.Any(pm => pm.UserId == userId) == true;

        if (!hasTaskAccess)
        {
            return new List<DocumentListItem>();
        }

        var documents = await _context.TaskDocuments
            .AsNoTracking()
            .Where(td => td.TaskId == taskId)
            .Select(td => td.Document)
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Where(d => !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync();

        return await MapDocumentsAsync(documents, userId);
    }

    public async Task<bool> AttachDocumentToTaskAsync(int taskId, int documentId, int userId)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .ThenInclude(p => p.ProjectMembers)
            .FirstOrDefaultAsync(t => t.TaskId == taskId);

        var document = await _context.Documents
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (task == null || document == null)
        {
            return false;
        }

        var hasTaskAccess = task.AssignedUserId == userId
            || task.CreatedByUserId == userId
            || task.Project?.ProjectManagerId == userId
            || task.Project?.ProjectMembers.Any(pm => pm.UserId == userId) == true;

        if (!hasTaskAccess || !await _authorizationService.CanViewDocumentAsync(document, userId))
        {
            return false;
        }

        var exists = await _context.TaskDocuments.AnyAsync(td => td.TaskId == taskId && td.DocumentId == documentId);
        if (!exists)
        {
            _context.TaskDocuments.Add(new TaskDocument
            {
                TaskId = taskId,
                DocumentId = documentId,
                AttachedAtUtc = DateTime.UtcNow,
                AttachedByUserId = userId
            });
        }

        if (!document.ProjectId.HasValue && task.ProjectId.HasValue)
        {
            document.ProjectId = task.ProjectId;
        }

        await _context.SaveChangesAsync();
        await _activityService.LogAsync(documentId, userId, DocumentActivityTypes.UpdateMetadata, $"Attached to task {taskId}");
        return true;
    }

    public async Task<DocumentDownloadResult?> GetDownloadAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);

        if (document == null || !await _authorizationService.CanViewDocumentAsync(document, userId))
        {
            return null;
        }

        var stream = await _fileStorageService.OpenReadAsync(document.StoredRelativePath, cancellationToken);
        await _activityService.LogAsync(document.DocumentId, userId, DocumentActivityTypes.Download);

        return new DocumentDownloadResult
        {
            Content = stream,
            ContentType = document.MimeType,
            FileName = document.OriginalFileName
        };
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, int userId, UpdateDocumentRequest request)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);
        if (document == null || !await _authorizationService.CanEditDocumentAsync(document, userId))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return false;
        }

        if (!DocumentCategories.All.Contains(request.Category))
        {
            return false;
        }

        document.Title = request.Title.Trim();
        document.Description = NormalizeNullable(request.Description);
        document.Category = request.Category;
        document.Tags = NormalizeTags(request.Tags);

        await _context.SaveChangesAsync();
        await _activityService.LogAsync(document.DocumentId, userId, DocumentActivityTypes.UpdateMetadata);
        return true;
    }

    public async Task<bool> ReplaceFileAsync(int documentId, ReplaceDocumentRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
        if (document == null || !await _authorizationService.CanEditDocumentAsync(document, request.RequestingUserId))
        {
            return false;
        }

        ValidateReplaceRequest(request);
        var previousPath = document.StoredRelativePath;
        var saveResult = await _fileStorageService.SaveAsync(request.RequestingUserId, document.ProjectId, request.OriginalFileName, content, cancellationToken);

        document.StoredRelativePath = saveResult.RelativePath;
        document.OriginalFileName = request.OriginalFileName;
        document.MimeType = NormalizeContentType(request.ContentType);
        document.FileSizeBytes = request.FileSizeBytes;

        await _context.SaveChangesAsync(cancellationToken);
        await _fileStorageService.DeleteAsync(previousPath, cancellationToken);
        await _activityService.LogAsync(document.DocumentId, request.RequestingUserId, DocumentActivityTypes.ReplaceFile);
        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int userId, string? reason = null)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);
        if (document == null || !await _authorizationService.CanDeleteDocumentAsync(document, userId))
        {
            return false;
        }

        await _fileStorageService.DeleteAsync(document.StoredRelativePath);

        document.IsDeleted = true;
        document.DeletedAtUtc = DateTime.UtcNow;
        document.DeletedByUserId = userId;
        document.StoredRelativePath = string.Empty;

        await _context.SaveChangesAsync();
        await _activityService.LogAsync(document.DocumentId, userId, DocumentActivityTypes.Delete, reason);
        return true;
    }

    public async Task<bool> ShareDocumentAsync(int documentId, DocumentShareRequest request)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null || !await _authorizationService.CanEditDocumentAsync(document, request.RequestingUserId))
        {
            return false;
        }

        var hasTarget = request.UserIds.Any() || request.ProjectId.HasValue;
        if (!hasTarget)
        {
            return false;
        }

        var distinctUserIds = request.UserIds.Where(id => id != request.RequestingUserId).Distinct().ToList();
        foreach (var userId in distinctUserIds)
        {
            var exists = await _context.DocumentShares.AnyAsync(ds => ds.DocumentId == documentId && ds.SharedWithUserId == userId);
            if (!exists)
            {
                _context.DocumentShares.Add(new DocumentShare
                {
                    DocumentId = documentId,
                    SharedByUserId = request.RequestingUserId,
                    SharedWithUserId = userId,
                    SharedAtUtc = DateTime.UtcNow,
                    Message = NormalizeNullable(request.Message)
                });
            }
        }

        if (request.ProjectId.HasValue)
        {
            var exists = await _context.DocumentShares.AnyAsync(ds => ds.DocumentId == documentId && ds.SharedWithProjectId == request.ProjectId);
            if (!exists)
            {
                _context.DocumentShares.Add(new DocumentShare
                {
                    DocumentId = documentId,
                    SharedByUserId = request.RequestingUserId,
                    SharedWithProjectId = request.ProjectId,
                    SharedAtUtc = DateTime.UtcNow,
                    Message = NormalizeNullable(request.Message)
                });
            }
        }

        await _context.SaveChangesAsync();

        foreach (var userId in distinctUserIds)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = userId,
                Title = "Document shared with you",
                Message = $"{document.Title} is now available in Shared with Me.",
                Type = NotificationType.DocumentShared,
                Priority = NotificationPriority.Important
            });
        }

        if (request.ProjectId.HasValue)
        {
            var projectMembers = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId != request.RequestingUserId)
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in projectMembers)
            {
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = userId,
                    Title = "Project document shared",
                    Message = $"{document.Title} was shared with your project team.",
                    Type = NotificationType.DocumentShared,
                    Priority = NotificationPriority.Informational
                });
            }
        }

        await _activityService.LogAsync(document.DocumentId, request.RequestingUserId, DocumentActivityTypes.Share);
        return true;
    }

    public async Task<List<DocumentListItem>> GetRecentDocumentsAsync(int userId, int take = 5)
    {
        var documents = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Where(d => !d.IsDeleted && d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Take(take)
            .ToListAsync();

        return await MapDocumentsAsync(documents, userId);
    }

    public Task<int> GetDocumentCountAsync(int userId)
    {
        return _context.Documents.CountAsync(d => !d.IsDeleted && d.UploadedByUserId == userId);
    }

    private IQueryable<Document> GetAccessibleDocumentsQuery(int userId)
    {
        var sharedToUserIds = _context.DocumentShares
            .Where(ds => ds.SharedWithUserId == userId)
            .Select(ds => ds.DocumentId);

        var sharedToProjectIds = from ds in _context.DocumentShares
                                 join pm in _context.ProjectMembers on ds.SharedWithProjectId equals pm.ProjectId
                                 where pm.UserId == userId
                                 select ds.DocumentId;

        var isAdmin = _context.Users.Any(u => u.UserId == userId && u.Role == UserRole.Administrator);

        var query = _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Include(d => d.Shares)
            .Where(d => !d.IsDeleted &&
                (isAdmin
                 || d.UploadedByUserId == userId
                 || (d.ProjectId.HasValue && _context.ProjectMembers.Any(pm => pm.ProjectId == d.ProjectId && pm.UserId == userId))
                 || sharedToUserIds.Contains(d.DocumentId)
                 || sharedToProjectIds.Contains(d.DocumentId)));

        return query;
    }

    private IQueryable<Document> ApplyOptions(IQueryable<Document> query, DocumentQueryOptions? options)
    {
        if (options == null)
        {
            return query.OrderByDescending(d => d.UploadedAtUtc);
        }

        if (!string.IsNullOrWhiteSpace(options.Category))
        {
            query = query.Where(d => d.Category == options.Category);
        }

        if (options.ProjectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == options.ProjectId.Value);
        }

        if (options.FromDate.HasValue)
        {
            query = query.Where(d => d.UploadedAtUtc >= options.FromDate.Value);
        }

        if (options.ToDate.HasValue)
        {
            query = query.Where(d => d.UploadedAtUtc <= options.ToDate.Value);
        }

        var sortBy = options.SortBy?.ToLowerInvariant() ?? "uploadedat";
        var ascending = string.Equals(options.Direction, "asc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, ascending) switch
        {
            ("title", true) => query.OrderBy(d => d.Title),
            ("title", false) => query.OrderByDescending(d => d.Title),
            ("category", true) => query.OrderBy(d => d.Category),
            ("category", false) => query.OrderByDescending(d => d.Category),
            ("size", true) => query.OrderBy(d => d.FileSizeBytes),
            ("size", false) => query.OrderByDescending(d => d.FileSizeBytes),
            ("uploadedat", true) => query.OrderBy(d => d.UploadedAtUtc),
            _ => query.OrderByDescending(d => d.UploadedAtUtc)
        };
    }

    private async Task<List<DocumentListItem>> MapDocumentsAsync(IEnumerable<Document> documents, int userId)
    {
        var list = new List<DocumentListItem>();
        foreach (var document in documents)
        {
            var item = await MapDocumentAsync(document, userId);
            list.Add(item);
        }

        return list;
    }

    private async Task<DocumentListItem> MapDocumentAsync(Document document, int userId)
    {
        var canEdit = await _authorizationService.CanEditDocumentAsync(document, userId);
        var canDelete = await _authorizationService.CanDeleteDocumentAsync(document, userId);

        return new DocumentListItem
        {
            DocumentId = document.DocumentId,
            Title = document.Title,
            Description = document.Description,
            Category = document.Category,
            Tags = document.Tags,
            OriginalFileName = document.OriginalFileName,
            MimeType = document.MimeType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedAtUtc = document.UploadedAtUtc,
            UploadedByUserId = document.UploadedByUserId,
            UploadedByDisplayName = document.UploadedByUser?.DisplayName ?? string.Empty,
            ProjectId = document.ProjectId,
            ProjectName = document.Project?.Name,
            SourceLabel = ResolveSourceLabel(document, userId),
            CanEdit = canEdit,
            CanDelete = canDelete,
            CanShare = canEdit
        };
    }

    private async Task<DocumentListItem?> BuildListItemAsync(int documentId, int userId, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        return document == null ? null : await MapDocumentAsync(document, userId);
    }

    private string ResolveSourceLabel(Document document, int userId)
    {
        if (document.UploadedByUserId == userId)
        {
            return "Mine";
        }

        if (document.Project != null)
        {
            return $"Project: {document.Project.Name}";
        }

        return "Shared";
    }

    private void ValidateUploadRequest(DocumentUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (!DocumentCategories.All.Contains(request.Category))
        {
            throw new InvalidOperationException("A valid category is required.");
        }

        ValidateFile(request.OriginalFileName, request.FileSizeBytes);
    }

    private void ValidateReplaceRequest(ReplaceDocumentRequest request)
    {
        ValidateFile(request.OriginalFileName, request.FileSizeBytes);
    }

    private void ValidateFile(string fileName, long fileSizeBytes)
    {
        if (fileSizeBytes <= 0 || fileSizeBytes > _storageOptions.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"Files must be between 1 byte and {_storageOptions.MaxFileSizeBytes} bytes.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !_storageOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported file type. Allowed types: {string.Join(", ", _storageOptions.AllowedExtensions)}");
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        return string.Join(",", tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string NormalizeContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
    }
}