using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Models;
using NuGetFeedTemplate.Services;
using SendGrid.Helpers.Mail;
using Microsoft.EntityFrameworkCore;

namespace NuGetFeedTemplate.Pages
{
    [Authorize]
    public class ApiKeysModel : PageModel
    {
        private string _siteName { get; }
        private ILogger<ApiKeysModel> _logger { get; }
        private FeedContext _dbContext { get; }

        public ApiKeysModel(ILogger<ApiKeysModel> logger, FeedContext dbContext, FeedSettings settings)
        {
            _logger = logger;
            _dbContext = dbContext;
            _siteName = settings.ServerName;
            AuthKeys = Array.Empty<AuthToken>();
        }

        public AuthToken GeneratedToken { get; set; }

        public IEnumerable<AuthToken> AuthKeys { get; set; }

        public int TotalKeys { get; set; }

        public int CurrentPage { get; set; }

        public bool HasNext { get; set; }

        public async Task OnGet(int page = 1)
        {
            if (!User.Identity.IsAuthenticated)
                return;

            CurrentPage = page;

            await RefreshKeys();
        }

        public async Task OnPost(
            [FromForm] TokenManagementRequest tokenRequest,
            [FromServices] IEmailService emailService)
        {
            switch (tokenRequest.Type)
            {
                case TokenRequestType.Create:
                    await OnCreate(tokenRequest.Description, emailService);
                    break;
                case TokenRequestType.Regenerate:
                    await OnRegenerate(tokenRequest.Id, emailService);
                    break;
                case TokenRequestType.Delete:
                    await OnDelete(tokenRequest.Id, emailService);
                    break;
            }

            var token = await _dbContext.AuthTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Key == tokenRequest.Id &&
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

        private async Task OnCreate(string description, IEmailService emailService)
        {
            var authToken = new AuthToken
            {
                Description = description,
                UserEmail = User.FindFirstValue("preferred_username")
            };
            var to = new EmailAddress(authToken.UserEmail, User.FindFirstValue("name"));
            if (!await _dbContext.Users.AnyAsync(x => x.Email == authToken.UserEmail))
            {
                var user = new User
                {
                    Email = authToken.UserEmail,
                    Name = User.FindFirstValue("name"),
                    PackagePublisher = !await _dbContext.Users.AnyAsync()
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
                await emailService.SendEmail(
                    EmailTemplates.WelcomeUser,
                    to,
                    $"Welcome to {_siteName}",
                    new WelcomeMessage
                    {
                        Host = $"{Request.Scheme}://{Request.Host}",
                        SiteName = _siteName,
                        Username = user.Email
                    });
            }

            _dbContext.AuthTokens.Add(authToken);
            await _dbContext.SaveChangesAsync();
            await emailService.SendEmail(
                EmailTemplates.TokenCreated,
                to,
                "New Token Created",
                new TokenAction
                {
                    Description = authToken.Description,
                    Expires = authToken.Expires.ToString("F")
                });

            GeneratedToken = authToken;
        }

        private async Task OnRegenerate(string tokenKey, IEmailService emailService)
        {
            var authToken = await _dbContext.AuthTokens
                .FirstOrDefaultAsync(x => x.Key == tokenKey && x.UserEmail == User.FindFirstValue("preferred_username"));

            if (authToken is null)
                return;

            if (authToken.IsValid())
            {
                authToken.Revoked = true;
                _dbContext.AuthTokens.Update(authToken);
            }

            var regeneratedToken = new AuthToken
            {
                Description = authToken.Description,
                UserEmail = authToken.UserEmail,
            };
            _dbContext.AuthTokens.Add(regeneratedToken);
            await _dbContext.SaveChangesAsync();

            var to = new EmailAddress(authToken.UserEmail, User.FindFirstValue("name"));
            await emailService.SendEmail(
                EmailTemplates.TokenRegenerated,
                to,
                "Token Regenerated",
                new TokenAction
                {
                    Description = authToken.Description,
                    Expires = authToken.Expires.ToString("F")
                });

            GeneratedToken = regeneratedToken;
        }

        private async Task OnDelete(string tokenKey, IEmailService emailService)
        {
            var authToken = await _dbContext.AuthTokens
                .FirstOrDefaultAsync(x => x.Key == tokenKey && x.UserEmail == User.FindFirstValue("preferred_username"));

            if (authToken is null)
                return;

            authToken.Revoked = true;
            _dbContext.AuthTokens.Update(authToken);
            await _dbContext.SaveChangesAsync();

            var to = new EmailAddress(authToken.UserEmail, User.FindFirstValue("name"));
            await emailService.SendEmail(
                EmailTemplates.TokenRevoked,
                to,
                "Token Revoked",
                new TokenAction
                {
                    Description = authToken.Description,
                    Expires = authToken.Expires.ToString("F")
                });
        }

        private async Task RefreshKeys()
        {
            var email = User.FindFirstValue("preferred_username");
            if (string.IsNullOrEmpty(email))
                return;

            if (CurrentPage < 1)
                CurrentPage = 1;

            TotalKeys = await _dbContext.AuthTokens
                .Where(x => x.UserEmail == User.FindFirstValue("preferred_username"))
                .CountAsync();

            var lastPage = (int)Math.Ceiling((double)TotalKeys / 10.0);

            if (CurrentPage > lastPage)
                CurrentPage = lastPage;

            HasNext = CurrentPage < lastPage;

            var skip = (CurrentPage - 1) * 10;

            AuthKeys = await _dbContext.AuthTokens
                .Where(x => x.UserEmail == email && x.Revoked == false)
                .Skip(skip)
                .Take(10)
                .ToArrayAsync();
        }
    }
}
