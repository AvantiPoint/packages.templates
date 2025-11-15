using System.ComponentModel.DataAnnotations;
using NuGetFeedTemplate.Models;

namespace NuGetFeedTemplate.Data.Models;

public class TokenNotification
{
    [Key]
    public int Id { get; set; }

    [MaxLength(32)]
    public string TokenKey { get; set; }

    public string NotificationType { get; set; }

    public DateTimeOffset SentAt { get; set; }

    public AuthToken Token { get; set; }

    public TokenNotificationType NotificationTypeEnum
    {
        get => Enum.TryParse<TokenNotificationType>(NotificationType, out var type) ? type : default;
        set => NotificationType = value.ToString();
    }
}
