using System.ComponentModel.DataAnnotations;

namespace NuGetFeedTemplate.Data.Models;

public class RefreshToken
{
    [Key]
    public string Token { get; set; }

    public string UserEmail { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset Expires { get; set; }

    public bool IsRevoked { get; set; }

    public string CreatedByIp { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string RevokedByIp { get; set; }

    public User User { get; set; }

    public bool IsValid => !IsRevoked && DateTimeOffset.Now < Expires;
}
