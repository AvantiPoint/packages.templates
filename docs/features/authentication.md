# Authentication

The template provides a comprehensive authentication system that combines Azure Active Directory for web UI access with API key-based authentication for NuGet client operations.

## Overview

The authentication system has two main components:

1. **Web Authentication** - Azure AD for browser-based access to the management UI
2. **API Authentication** - API keys for NuGet client operations (push, restore, etc.)

## Web Authentication (Azure AD)

### How It Works

Users access the web UI through Azure Active Directory:

1. User navigates to the feed URL
2. Application redirects to Azure AD login
3. User signs in with organizational credentials
4. Azure AD redirects back with authentication token
5. Application creates user account (if first time)
6. User accesses feed management UI

### Configuration

Configure Azure AD in `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourcompany.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CallbackPath": "/signin-oidc"
  }
}
```

### Account Selection

The template is configured to prompt users to select their account on each login:

```csharp
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Prompt = "select_account";
});
```

This improves security by:
- Preventing unintended account usage
- Making it clear which account is being used
- Supporting users with multiple organizational accounts

### First User Privilege

The first user to sign in is automatically granted **Publisher** privileges:

```csharp
user = new User
{
    Email = email,
    Name = ctx.Principal.FindFirstValue("name"),
    PackagePublisher = !await feedContext.Users.AnyAsync() // true if first user
};
```

Additional users are created as **Consumers** by default.

## API Authentication

### How It Works

NuGet clients authenticate using API keys:

1. User creates API key through web UI
2. User configures NuGet client with API key
3. NuGet client sends API key in request header
4. Application validates key and permissions
5. Request is authorized or rejected

### Creating API Keys

Users create API keys through the web interface:

1. Sign in to the web UI
2. Navigate to **Account** → **API Keys**
3. Click **Create New Token**
4. Provide a description (e.g., "Build Server", "Dev Machine")
5. Copy the generated key (shown only once)

### API Key Security

API keys are stored securely:

- **Hashed in Database** - Only hash is stored, not the actual key
- **User-Scoped** - Each key is tied to a specific user
- **Revocable** - Keys can be revoked at any time
- **Auditable** - All key usage is logged

### Using API Keys

Configure NuGet client with your API key:

```bash
# Add source with credentials
dotnet nuget add source https://your-feed.com/v3/index.json \
  --name MyFeed \
  --username your-email@company.com \
  --password your-api-key \
  --store-password-in-clear-text

# Push a package
dotnet nuget push MyPackage.1.0.0.nupkg \
  --source MyFeed \
  --api-key your-api-key
```

Or in `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="MyFeed" value="https://your-feed.com/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <MyFeed>
      <add key="Username" value="your-email@company.com" />
      <add key="ClearTextPassword" value="your-api-key" />
    </MyFeed>
  </packageSourceCredentials>
</configuration>
```

## Roles and Permissions

The template defines two roles:

### Consumer Role

**Permissions:**
- Download packages
- Search packages
- View package metadata
- View own API keys

**Cannot:**
- Upload packages
- Delete packages
- Manage other users

All authenticated users have the Consumer role by default.

### Publisher Role

**Permissions:**
- All Consumer permissions
- Upload packages
- Delete packages (unlist)
- Manage package groups
- Configure syndication targets
- View all users

**Assigned to:**
- First user (automatically)
- Users manually granted Publisher status

### Role Implementation

Roles are defined in `FeedRoles.cs`:

```csharp
public static class FeedRoles
{
    public const string Consumer = "Consumer";
    public const string Publisher = "Publisher";
}
```

And checked in `PackageAuthenticationService.cs`:

```csharp
if (token.User.PackagePublisher)
{
    identity.AddClaim(new Claim(ClaimTypes.Role, FeedRoles.Publisher));
}
```

## Package Authentication Service

The core authentication logic is in `PackageAuthenticationService.cs`, which implements `IPackageAuthenticationService`.

### API Key Authentication

```csharp
public async Task<NuGetAuthenticationResult> AuthenticateAsync(
    string apiKey, 
    CancellationToken cancellationToken)
{
    var authToken = await _dbContext.AuthTokens
        .Include(x => x.User)
        .FirstOrDefaultAsync(x => 
            x.Key == apiKey && 
            x.Revoked == false && 
            x.User.PackagePublisher == true);
    
    return CreateResult(authToken, false);
}
```

### Username/Token Authentication

```csharp
public async Task<NuGetAuthenticationResult> AuthenticateAsync(
    string username, 
    string token, 
    CancellationToken cancellationToken)
{
    var authToken = await _dbContext.AuthTokens
        .Include(x => x.User)
        .FirstOrDefaultAsync(x => 
            x.Key == token && 
            x.User.Email == username && 
            x.Revoked == false);

    return CreateResult(authToken, true);
}
```

### Creating Claims

Successful authentication creates a `ClaimsPrincipal`:

```csharp
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

    return NuGetAuthenticationResult.Success(new ClaimsPrincipal(identity));
}
```

## Custom Claims

The template defines custom claims in `FeedClaims.cs`:

```csharp
public static class FeedClaims
{
    public const string Token = "feed:token";
    public const string TokenDescription = "feed:token:description";
}
```

These claims are used for:
- Tracking which API key was used
- Including token info in email notifications
- Auditing API key usage

## Security Features

### IP Address Tracking

Every authentication logs the client IP:

```csharp
_logger.LogInformation($"Authenticated user: {token.User.Name} from {Connection.RemoteIpAddress}.");
```

Failed authentications are also logged:

```csharp
_logger.LogWarning($"Failed login from {Connection.RemoteIpAddress} (Realm: {realm})\n{message}");
```

### Token Validation

Tokens are validated before use:

```csharp
public bool IsValid()
{
    return !Revoked && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}
```

Tokens can be:
- **Revoked** - Manually disabled by user
- **Expired** - Past expiration date (if set)

### Failed Login Handling

Failed logins return appropriate HTTP status codes:

- **401 Unauthorized** - Invalid credentials
- **WWW-Authenticate header** - Prompts NuGet client for credentials

## Customizing Authentication

### Adding Custom Claims

Modify `PackageAuthenticationService.cs` to add custom claims:

```csharp
identity.AddClaim(new Claim("Department", token.User.Department));
identity.AddClaim(new Claim("EmployeeId", token.User.EmployeeId));
```

### Custom Authorization Logic

Implement custom authorization in `NuGetFeedActionHandler.cs`:

```csharp
public Task<bool> CanDownloadPackage(string packageId, string version)
{
    // Custom logic here
    if (packageId.StartsWith("Internal."))
    {
        return Task.FromResult(User.HasClaim("Department", "Engineering"));
    }
    
    return Task.FromResult(User.IsInRole(FeedRoles.Consumer));
}
```

### Alternative Identity Providers

To use a different identity provider instead of Azure AD:

1. Remove the Azure AD configuration from `Program.cs`
2. Add your identity provider's authentication middleware
3. Update the `OnTokenValidated` handler to match your claims structure

Example for IdentityServer4:

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies")
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "https://your-identity-server.com";
    options.ClientId = "nuget-feed";
    options.ClientSecret = "secret";
    options.ResponseType = "code";
    options.SaveTokens = true;
});
```

## Best Practices

1. **Rotate API Keys Regularly** - Create new keys and revoke old ones periodically
2. **Use Descriptive Names** - Name keys by purpose (e.g., "CI Build", "Dev Machine")
3. **Revoke Compromised Keys** - Immediately revoke if key is exposed
4. **Limit Publisher Role** - Only grant Publisher to users who need to upload
5. **Monitor Failed Logins** - Watch logs for authentication failures
6. **Use HTTPS** - Always use HTTPS in production to protect credentials
7. **Secure Key Storage** - Store API keys securely in build systems

## Troubleshooting

### "Invalid Token or Credentials"

**Causes:**
- API key is incorrect
- Token has been revoked
- Token has expired
- User doesn't have Publisher role (for package push)

**Solution:**
- Verify the API key is correct
- Check token status in web UI
- Create a new token if needed

### "Failed to sign in"

**Causes:**
- Incorrect Azure AD configuration
- User not in tenant
- MFA required but not completed

**Solution:**
- Verify TenantId and ClientId
- Check user's Azure AD access
- Complete MFA if required

### "Forbidden" on package download

**Causes:**
- Anonymous access disabled
- User doesn't have Consumer role
- API key is invalid

**Solution:**
- Enable anonymous access if needed
- Verify user authentication
- Check API key is valid

## Next Steps

- [Configure User Management](user-management.md)
- [Set up Email Notifications](email-notifications.md)
- [Learn about Extensibility](../advanced/extensibility.md)
