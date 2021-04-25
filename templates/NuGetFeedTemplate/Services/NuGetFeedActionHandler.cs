using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AvantiPoint.Packages.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NuGetFeedTemplate.Authentication;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Models;
using SendGrid.Helpers.Mail;

namespace NuGetFeedTemplate.Services
{
    public class NuGetFeedActionHandler : INuGetFeedActionHandler
    {
        private FeedContext _dbContext { get; }
        private IEmailService _emailService { get; }
        private ILogger _logger { get; }

        public NuGetFeedActionHandler(
            IHttpContextAccessor contextAccessor,
            IEmailService emailService,
            FeedContext dbContext,
            ILogger<NuGetFeedActionHandler> logger)
        {
            HttpContext = contextAccessor.HttpContext;
            _dbContext = dbContext;
            _emailService = emailService;
            _logger = logger;

            UserAgent = HttpContext.Request.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null;
            RequestIP = HttpContext.Connection.RemoteIpAddress.ToString();
        }

        public HttpContext HttpContext { get; }

        public ClaimsPrincipal User => HttpContext.User;

        public string UserAgent { get; }

        public string RequestIP { get; }

        public Task<bool> CanDownloadPackage(string packageId, string version)
        {
            return Task.FromResult(User.IsInRole(FeedRoles.Consumer));
        }

        public async Task OnPackageDownloaded(string packageId, string version)
        {
            try
            {
                _logger.LogInformation($"{User.Identity.Name} downloaded {packageId}.{version}.nupkg");
                var download = new PackageDownload
                {
                    AuthTokenKey = User.FindFirstValue(FeedClaims.Token),
                    IPAddress = RequestIP,
                    PackageId = packageId,
                    PackageVersion = version,
                    UserAgent = UserAgent
                };

                var sendFirstUseEmail = !await _dbContext.Downloads.AnyAsync(x => x.IPAddress == RequestIP && x.AuthTokenKey == download.AuthTokenKey);
                _dbContext.Downloads.Add(download);
                await _dbContext.SaveChangesAsync();
                if(sendFirstUseEmail)
                    await SendEmail(EmailTemplates.TokenFirstUse, "Token used from new IP Address", packageId, version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error handling OnPackageDownloaded - {packageId} {version}");
            }
            
        }

        public Task OnPackageUploaded(string packageId, string version)
        {
            _logger.LogInformation($"{User.Identity.Name} uploaded {packageId}.{version}.nupkg");
            return SendEmail(EmailTemplates.PackageUploaded, $"Package Uploaded - {packageId} {version}", packageId, version);
        }

        public Task OnSymbolsDownloaded(string packageId, string version)
        {
            return Task.CompletedTask;
        }

        public Task OnSymbolsUploaded(string packageId, string version)
        {
            _logger.LogInformation($"{User.Identity.Name} uploaded {packageId}.{version}.snupkg");
            return SendEmail(EmailTemplates.SymbolsUploaded, $"Symbols Uploaded - {packageId} {version}", packageId, version);
        }

        private async Task SendEmail(string templateId, string subject, string packageId, string version)
        {
            var context = CreatePackageAction(packageId, version);
            var to = new EmailAddress(User.FindFirstValue(ClaimTypes.Email), User.FindFirstValue(ClaimTypes.Name));
            await _emailService.SendEmail(templateId, to, subject, context);
        }

        private PackageAction CreatePackageAction(string packageId, string version)
        {
            return new PackageAction
            {
                Id = packageId,
                Version = version,
                IPAddress = RequestIP,
                TokenDescription = User.FindFirstValue(FeedClaims.TokenDescription),
                UserAgent = UserAgent
            };
        }
    }
}
