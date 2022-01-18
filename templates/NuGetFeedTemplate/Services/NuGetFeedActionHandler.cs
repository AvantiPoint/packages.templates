using System;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using AvantiPoint.Packages.Core;
using AvantiPoint.Packages.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using NuGetFeedTemplate.Authentication;
using NuGetFeedTemplate.Models;

namespace NuGetFeedTemplate.Services
{
    public class NuGetFeedActionHandler : INuGetFeedActionHandler
    {
        private IEmailService _emailService { get; }
        private ILogger _logger { get; }
        private ISyndicationService _syndicationService { get; }
        private IContext _context { get; }

        public NuGetFeedActionHandler(
            IHttpContextAccessor contextAccessor,
            IEmailService emailService,
            IContext context,
            ISyndicationService syndicationService,
            ILogger<NuGetFeedActionHandler> logger)
        {
            HttpContext = contextAccessor.HttpContext;
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _syndicationService = syndicationService;

            UserAgent = HttpContext.Request.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null;
            RemoteIp = HttpContext.Connection.RemoteIpAddress;
            RequestIP = RemoteIp.ToString();
        }

        public HttpContext HttpContext { get; }

        public ClaimsPrincipal User => HttpContext.User;

        public string UserAgent { get; }

        public IPAddress RemoteIp { get; }

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

                if(await _context.PackageDownloads.CountAsync(x => x.RemoteIp == RemoteIp) == 1)
                    await SendEmail(EmailTemplates.TokenFirstUse, "Token used from new IP Address", packageId, version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error handling OnPackageDownloaded - {packageId} {version}");
            }
        }

        public async Task OnPackageUploaded(string packageId, string version)
        {
            _logger.LogInformation($"{User.Identity.Name} uploaded {packageId}.{version}.nupkg");
            await SendEmail(EmailTemplates.PackageUploaded, $"Package Uploaded - {packageId} {version}", packageId, version);

            await _syndicationService.SyndicatePackage(packageId, NuGetVersion.Parse(version));
        }

        public Task OnSymbolsDownloaded(string packageId, string version)
        {
            return Task.CompletedTask;
        }

        public async Task OnSymbolsUploaded(string packageId, string version)
        {
            _logger.LogInformation($"{User.Identity.Name} uploaded {packageId}.{version}.snupkg");
            await SendEmail(EmailTemplates.SymbolsUploaded, $"Symbols Uploaded - {packageId} {version}", packageId, version);

            await _syndicationService.SyndicateSymbols(packageId, NuGetVersion.Parse(version));
        }

        private async Task SendEmail(string templateId, string subject, string packageId, string version)
        {
            var context = CreatePackageAction(packageId, version);
            var to = new MailAddress(User.FindFirstValue(ClaimTypes.Email), User.FindFirstValue(ClaimTypes.Name));
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
