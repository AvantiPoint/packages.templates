using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Services;

namespace NuGetFeedTemplate.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class SyndicationController : ControllerBase
{
    private FeedContext _feedContext { get; }
    private ISyndicationService _syndicationService { get; }

    public SyndicationController(FeedContext feedContext, ISyndicationService syndicationService)
    {
        _feedContext = feedContext;
        _syndicationService = syndicationService;
    }

    [HttpPost("group/{groupName}/target/{targetName}")]
    public async Task<IActionResult> PushToSource(string groupName, string targetName, [FromHeader(Name = "X-ApiKey")] string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return Unauthorized();

        var user = await _feedContext.Users
            .Include(x => x.Tokens)
            .FirstOrDefaultAsync(x => x.Tokens.Any(t => t.Key == apiKey));

        if (user is null || user.Tokens.FirstOrDefault(x => x.Key == apiKey && x.IsValid()) is null)
            return Unauthorized();

        await _syndicationService.PushToSource(groupName, targetName);

        return Ok();
    }
}
