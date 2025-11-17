using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;

namespace NuGetFeedTemplate.Services;

public interface IJwtTokenService
{
    Task<(string accessToken, string refreshToken)> GenerateTokensAsync(User user, string ipAddress, string userAgent = null, string deviceInfo = null);
    Task<(string accessToken, string refreshToken)?> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent = null, string deviceInfo = null);
    Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<bool> RevokeAllUserTokensAsync(string userEmail, string ipAddress);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly FeedContext _dbContext;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(
        JwtSettings jwtSettings,
        FeedContext dbContext,
        ILogger<JwtTokenService> logger)
    {
        _jwtSettings = jwtSettings;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(User user, string ipAddress, string userAgent = null, string deviceInfo = null)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user, ipAddress, userAgent, deviceInfo);
        
        return (accessToken, refreshToken);
    }

    public async Task<(string accessToken, string refreshToken)?> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent = null, string deviceInfo = null)
    {
        var token = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (token == null || !token.IsValid || token.User.IsRevoked)
        {
            _logger.LogWarning("Invalid refresh token attempt from IP: {IpAddress}", ipAddress);
            return null;
        }

        // Revoke old refresh token
        token.IsRevoked = true;
        token.RevokedAt = DateTimeOffset.Now;
        token.RevokedByIp = ipAddress;

        // Update last login
        token.User.LastLoginAt = DateTimeOffset.Now;

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(token.User);
        var newRefreshToken = await GenerateRefreshTokenAsync(token.User, ipAddress, userAgent, deviceInfo);

        await _dbContext.SaveChangesAsync();

        return (newAccessToken, newRefreshToken);
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (token == null || token.IsRevoked)
            return false;

        token.IsRevoked = true;
        token.RevokedAt = DateTimeOffset.Now;
        token.RevokedByIp = ipAddress;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(string userEmail, string ipAddress)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(x => x.UserEmail == userEmail && !x.IsRevoked)
            .ToListAsync();

        if (!tokens.Any())
            return false;

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTimeOffset.Now;
            token.RevokedByIp = ipAddress;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.PackagePublisher)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(User user, string ipAddress, string userAgent = null, string deviceInfo = null)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = GenerateSecureToken(),
            UserEmail = user.Email,
            Expires = DateTimeOffset.Now.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = deviceInfo
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return refreshToken.Token;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
