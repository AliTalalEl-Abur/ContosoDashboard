using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentAuthorizationService
{
    Task<bool> IsAdministratorAsync(int userId);
    Task<bool> CanAccessProjectAsync(int projectId, int userId);
    Task<bool> CanViewDocumentAsync(Document document, int userId);
    Task<bool> CanEditDocumentAsync(Document document, int userId);
    Task<bool> CanDeleteDocumentAsync(Document document, int userId);
}

public class DocumentAuthorizationService : IDocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;

    public DocumentAuthorizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsAdministratorAsync(int userId)
    {
        return await _context.Users
            .AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator);
    }

    public async Task<bool> CanAccessProjectAsync(int projectId, int userId)
    {
        return await _context.Projects
            .AnyAsync(p => p.ProjectId == projectId &&
                (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)));
    }

    public async Task<bool> CanViewDocumentAsync(Document document, int userId)
    {
        if (await IsAdministratorAsync(userId))
        {
            return true;
        }

        if (document.UploadedByUserId == userId)
        {
            return true;
        }

        if (document.ProjectId.HasValue && await CanAccessProjectAsync(document.ProjectId.Value, userId))
        {
            return true;
        }

        return await _context.DocumentShares.AnyAsync(ds => ds.DocumentId == document.DocumentId &&
            (ds.SharedWithUserId == userId
                || (ds.SharedWithProjectId.HasValue && _context.ProjectMembers.Any(pm => pm.ProjectId == ds.SharedWithProjectId && pm.UserId == userId))));
    }

    public async Task<bool> CanEditDocumentAsync(Document document, int userId)
    {
        if (await IsAdministratorAsync(userId))
        {
            return true;
        }

        return document.UploadedByUserId == userId;
    }

    public async Task<bool> CanDeleteDocumentAsync(Document document, int userId)
    {
        if (await IsAdministratorAsync(userId) || document.UploadedByUserId == userId)
        {
            return true;
        }

        if (!document.ProjectId.HasValue)
        {
            return false;
        }

        return await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId);
    }
}
