using System.ComponentModel.DataAnnotations;

namespace NuGetFeedTemplate.Data.Models;

public class TokenExpirationNotification
{
    [Key]
    public int Id { get; set; }

    [MaxLength(32)]
    public string TokenKey { get; set; }

    public string NotificationType { get; set; } // "7Days", "3Days", "Expired"

    public DateTimeOffset SentAt { get; set; }

    public AuthToken Token { get; set; }
}
