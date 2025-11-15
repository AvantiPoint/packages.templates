using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Models;

namespace NuGetFeedTemplate.Services;

public class TokenExpirationNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenExpirationNotificationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public TokenExpirationNotificationService(
        IServiceProvider serviceProvider,
        ILogger<TokenExpirationNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Expiration Notification Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckTokenExpirations(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking token expirations.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Token Expiration Notification Service is stopping.");
    }

    private async Task CheckTokenExpirations(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FeedContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTimeOffset.Now;
        var sevenDaysFromNow = now.AddDays(7);
        var threeDaysFromNow = now.AddDays(3);

        // Get all non-revoked, non-system tokens
        var tokens = await context.AuthTokens
            .Include(t => t.User)
            .Where(t => !t.Revoked && !t.IsSystemToken)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Check if token has expired
                if (token.Expires <= now)
                {
                    await SendExpirationNotification(token, "Expired", EmailTemplates.TokenExpired, 
                        "Token Expired", context, emailService, cancellationToken);
                }
                // Check if token expires in 3 days
                else if (token.Expires <= threeDaysFromNow && token.Expires > now)
                {
                    await SendExpirationNotification(token, "3Days", EmailTemplates.TokenExpiring3Days,
                        "Token Expiring in 3 Days", context, emailService, cancellationToken);
                }
                // Check if token expires in 7 days
                else if (token.Expires <= sevenDaysFromNow && token.Expires > threeDaysFromNow)
                {
                    await SendExpirationNotification(token, "7Days", EmailTemplates.TokenExpiring7Days,
                        "Token Expiring in 7 Days", context, emailService, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing token expiration for token {token.Key}");
            }
        }
    }

    private async Task SendExpirationNotification(
        AuthToken token,
        string notificationType,
        string templateName,
        string subject,
        FeedContext context,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        // Check if notification was already sent
        var alreadySent = await context.TokenExpirationNotifications
            .AnyAsync(n => n.TokenKey == token.Key && n.NotificationType == notificationType, cancellationToken);

        if (alreadySent)
        {
            _logger.LogDebug($"Notification '{notificationType}' already sent for token {token.Key}");
            return;
        }

        // Send email
        var daysRemaining = (int)(token.Expires - DateTimeOffset.Now).TotalDays;
        var emailContext = new TokenExpiration
        {
            Description = token.Description,
            Expires = token.Expires.ToString("F"),
            DaysRemaining = Math.Max(0, daysRemaining)
        };

        var to = new MailAddress(token.User.Email, token.User.Name);
        var success = await emailService.SendEmail(templateName, to, subject, emailContext);

        if (success)
        {
            // Record that notification was sent
            var notification = new TokenExpirationNotification
            {
                TokenKey = token.Key,
                NotificationType = notificationType,
                SentAt = DateTimeOffset.Now
            };

            context.TokenExpirationNotifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Sent '{notificationType}' notification for token {token.Key} to {token.User.Email}");
        }
        else
        {
            _logger.LogWarning($"Failed to send '{notificationType}' notification for token {token.Key} to {token.User.Email}");
        }
    }
}
