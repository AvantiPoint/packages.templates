namespace NuGetFeedTemplate.Models;

public enum TokenNotificationType
{
    TokenCreated,
    TokenRevoked,
    TokenRegenerated,
    TokenFirstUse,
    TokenExpiring7Days,
    TokenExpiring3Days,
    TokenExpired
}
