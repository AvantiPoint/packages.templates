namespace NuGetFeedTemplate.Models;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string Name,
    bool IsAdmin);
