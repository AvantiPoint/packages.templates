﻿using System.Security.Claims;
using AvantiPoint.Packages.Core;
using Microsoft.EntityFrameworkCore;
using NuGetFeedTemplate.Configuration;
using NuGetFeedTemplate.Data;
using NuGetFeedTemplate.Data.Models;

namespace NuGetFeedTemplate.Authentication;

public class PackageAuthenticationService : IPackageAuthenticationService
{
    private FeedContext _dbContext { get; }
    private FeedSettings _settings { get; }
    private ILogger _logger { get; }

    public PackageAuthenticationService(
        FeedContext dbContext,
        FeedSettings settings,
        IHttpContextAccessor contextAccessor,
        ILogger<PackageAuthenticationService> logger)
    {
        _dbContext = dbContext;
        _settings = settings;
        _logger = logger;
        HttpContext = contextAccessor.HttpContext;
    }

    public HttpContext HttpContext { get; }

    public ConnectionInfo Connection => HttpContext.Connection;

    public async Task<NuGetAuthenticationResult> AuthenticateAsync(string apiKey, CancellationToken cancellationToken)
    {
        var authToken = await _dbContext.AuthTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Key == apiKey && x.Revoked == false && x.User.PackagePublisher == true);
        return CreateResult(authToken, false);
    }

    public async Task<NuGetAuthenticationResult> AuthenticateAsync(string username, string token, CancellationToken cancellationToken)
    {
        var authToken = await _dbContext.AuthTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Key == token && x.User.Email == username && x.Revoked == false);

        return CreateResult(authToken, true);
    }

    private NuGetAuthenticationResult CreateResult(AuthToken token, bool includeRealm)
    {
        if (token is null || !token.IsValid())
            return Fail("Invalid Token or Credentials", includeRealm);

        var identity = new ClaimsIdentity("GitHub Auth");
        identity.AddClaim(new Claim(ClaimTypes.Name, token.User.Name));
        identity.AddClaim(new Claim(ClaimTypes.Email, token.User.Email));
        identity.AddClaim(new Claim(FeedClaims.Token, token.Key));
        identity.AddClaim(new Claim(FeedClaims.TokenDescription, token.Description));
        identity.AddClaim(new Claim(ClaimTypes.Role, FeedRoles.Consumer));

        if (token.User.PackagePublisher)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, FeedRoles.Publisher));
        }

        _logger.LogInformation($"Authenticated user: {token.User.Name} from {Connection.RemoteIpAddress}.");
        return NuGetAuthenticationResult.Success(new ClaimsPrincipal(identity));
    }

    private NuGetAuthenticationResult Fail(string message, bool includeRealm)
    {
        var realm = includeRealm ? $"{_settings.ServerName} Package Registry" : null;

        _logger.LogWarning($"Failed login from {Connection.RemoteIpAddress} (Realm: {realm})\n{message}");
        return NuGetAuthenticationResult.Fail(message, _settings.ServerName, realm);
    }
}
