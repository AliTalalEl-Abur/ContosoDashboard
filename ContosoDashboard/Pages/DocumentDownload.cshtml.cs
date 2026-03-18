using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using ContosoDashboard.Services;

namespace ContosoDashboard.Pages;

[Authorize]
public class DocumentDownloadModel : PageModel
{
    private readonly IDocumentService _documentService;

    public DocumentDownloadModel(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<IActionResult> OnGetAsync(int documentId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var download = await _documentService.GetDownloadAsync(documentId, userId, cancellationToken);
        if (download == null)
        {
            return Forbid();
        }

        return File(download.Content, download.ContentType, download.FileName);
    }
}
