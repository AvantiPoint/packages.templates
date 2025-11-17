using System;
using System.Collections.Generic;

namespace NuGetFeedTemplate.Data.Models;

public class User
{
    public string Name { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; }
    public string ProfilePictureUrl { get; set; }
    public bool PackagePublisher { get; set; }
    public bool IsRevoked { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public List<AuthToken> Tokens { get; set; }
    public List<RefreshToken> RefreshTokens { get; set; }
}
