using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Models;
using NuGetFeedTemplate.Services;
using SendGrid.Helpers.Mail;

namespace NuGetFeedTemplate.Pages
{
    public class IndexModel : PageModel
    {
        private ILogger<IndexModel> _logger { get; }
        private FeedContext _dbContext { get; }

        public IndexModel(ILogger<IndexModel> logger, FeedContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            AuthKeys = Array.Empty<AuthToken>();
        }

        public IEnumerable<AuthToken> AuthKeys { get; set; }

        public async Task OnGet()
        {
            if (!User.Identity.IsAuthenticated)
                return;

            await RefreshKeys();
        }

        public async Task OnPost(
            [Bind("Key")]AuthToken revokeToken,
            [FromServices]IEmailService emailService)
        {
            var token = await _dbContext.AuthTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Key == revokeToken.Key &&
                    x.UserEmail == User.FindFirstValue("preferred_username"));

            if (token != null && DateTimeOffset.Now < token.Expires)
            {
                token.Revoked = true;
                _dbContext.AuthTokens.Update(token);
                await _dbContext.SaveChangesAsync();
                await emailService.SendEmail(
                    EmailTemplates.TokenRevoked,
                    new EmailAddress(token.User.Email, token.User.Name),
                    "Auth Token Revoked",
                    new TokenAction
                    {
                        Description = token.Description,
                        IPAddress = HttpContext.Connection.RemoteIpAddress.ToString()
                    });
                _logger.LogInformation($"{token.User.Name} has revoked the auth token: '{token.Description}'.");
            }

            await RefreshKeys();
        }

        private async Task RefreshKeys()
        {
            var email = User.FindFirstValue("preferred_username");
            if (string.IsNullOrEmpty(email))
                return;

            AuthKeys = await _dbContext.AuthTokens
                .Where(x => x.UserEmail == email)
                .ToArrayAsync();
        }
    }
}
