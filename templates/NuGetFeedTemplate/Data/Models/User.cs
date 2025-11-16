using System;
using System.Collections.Generic;

namespace NuGetFeedTemplate.Data.Models;

public class User
{
    public string Name { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string ProfilePictureUrl { get; set; }
    public bool PackagePublisher { get; set; }
    public bool IsRevoked { get; set; }

    // For local authentication
    public string PasswordHash { get; set; }

    // For OAuth authentication
    public string ExternalProvider { get; set; }  // "Microsoft" or "Google"
    public string ExternalId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public List<AuthToken> Tokens { get; set; }
    public List<RefreshToken> RefreshTokens { get; set; }
}
