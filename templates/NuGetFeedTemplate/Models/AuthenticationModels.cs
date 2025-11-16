namespace NuGetFeedTemplate.Models;

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public bool IsAdmin { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; }
}

public class ExternalAuthCallbackRequest
{
    public string Code { get; set; }
    public string State { get; set; }
    public string Error { get; set; }
}
