using System.Net.Mail;
using Cronos;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;
using NuGetFeedTemplate.Models;

namespace NuGetFeedTemplate.Services;

public class TokenExpirationNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenExpirationNotificationService> _logger;
    private readonly CronExpression _cronExpression;

    public TokenExpirationNotificationService(
        IServiceProvider serviceProvider,
        ILogger<TokenExpirationNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        // Run daily at 6am UTC
        _cronExpression = CronExpression.Parse("0 6 * * *");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Expiration Notification Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = _cronExpression.GetNextOccurrence(now, TimeZoneInfo.Utc);

            if (nextRun.HasValue)
            {
                var delay = nextRun.Value - now;
                _logger.LogInformation($"Next token expiration check scheduled for {nextRun.Value:yyyy-MM-dd HH:mm:ss} UTC");

                try
                {
                    await Task.Delay(delay, stoppingToken);
                    
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await CheckTokenExpirations(stoppingToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected when service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while checking token expirations.");
                }
            }
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
                    await SendExpirationNotification(token, TokenNotificationType.TokenExpired, 
                        EmailTemplates.TokenExpired, "Token Expired", context, emailService, cancellationToken);
                }
                // Check if token expires in 3 days
                else if (token.Expires <= threeDaysFromNow && token.Expires > now)
                {
                    await SendExpirationNotification(token, TokenNotificationType.TokenExpiring3Days,
                        EmailTemplates.TokenExpiring3Days, "Token Expiring in 3 Days", context, emailService, cancellationToken);
                }
                // Check if token expires in 7 days
                else if (token.Expires <= sevenDaysFromNow && token.Expires > threeDaysFromNow)
                {
                    await SendExpirationNotification(token, TokenNotificationType.TokenExpiring7Days,
                        EmailTemplates.TokenExpiring7Days, "Token Expiring in 7 Days", context, emailService, cancellationToken);
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
        TokenNotificationType notificationType,
        string templateName,
        string subject,
        FeedContext context,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var notificationTypeString = notificationType.ToString();
        
        // Check if notification was already sent
        var alreadySent = await context.TokenNotifications
            .AnyAsync(n => n.TokenKey == token.Key && n.NotificationType == notificationTypeString, cancellationToken);

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
            var notification = new TokenNotification
            {
                TokenKey = token.Key,
                NotificationTypeEnum = notificationType,
                SentAt = DateTimeOffset.Now
            };

            context.TokenNotifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Sent '{notificationType}' notification for token {token.Key} to {token.User.Email}");
        }
        else
        {
            _logger.LogWarning($"Failed to send '{notificationType}' notification for token {token.Key} to {token.User.Email}");
        }
    }
}
