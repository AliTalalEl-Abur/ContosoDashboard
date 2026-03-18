using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentActivityService
{
    Task LogAsync(int documentId, int actorUserId, string actionType, string? details = null);
    Task<List<DocumentActivity>> GetActivityAsync(int requestingUserId, string? actionType = null, int? actorUserId = null, int? documentId = null, DateTime? from = null, DateTime? to = null);
}

public class DocumentActivityService : IDocumentActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentAuthorizationService _authorizationService;

    public DocumentActivityService(ApplicationDbContext context, IDocumentAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    public async Task LogAsync(int documentId, int actorUserId, string actionType, string? details = null)
    {
        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = documentId,
            ActorUserId = actorUserId,
            ActionType = actionType,
            DetailsJson = details,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<List<DocumentActivity>> GetActivityAsync(int requestingUserId, string? actionType = null, int? actorUserId = null, int? documentId = null, DateTime? from = null, DateTime? to = null)
    {
        if (!await _authorizationService.IsAdministratorAsync(requestingUserId))
        {
            return new List<DocumentActivity>();
        }

        var query = _context.DocumentActivities
            .Include(a => a.Document)
            .Include(a => a.ActorUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(a => a.ActionType == actionType);
        }

        if (actorUserId.HasValue)
        {
            query = query.Where(a => a.ActorUserId == actorUserId.Value);
        }

        if (documentId.HasValue)
        {
            query = query.Where(a => a.DocumentId == documentId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(a => a.OccurredAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.OccurredAtUtc <= to.Value);
        }

        return await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(200)
            .ToListAsync();
    }
}
