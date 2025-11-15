# Azure Active Directory Integration

This guide explains how to set up and configure Azure Active Directory (Azure AD) authentication for your NuGet feed.

## Overview

The template uses Azure AD for web-based authentication, allowing users to sign in with their organizational credentials. This provides:

- Single Sign-On (SSO) with organizational accounts
- Multi-Factor Authentication (MFA) support
- Centralized user management
- Secure OAuth 2.0 / OpenID Connect authentication

## Prerequisites

- Azure subscription
- Azure AD tenant (included with Microsoft 365, Office 365, or standalone)
- Global Administrator or Application Administrator role in Azure AD

## Setting Up Azure AD Application

### Step 1: Register Application

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory**
3. Select **App registrations** from the left menu
4. Click **New registration**

### Step 2: Configure Basic Settings

On the registration page:

**Name:** Enter a descriptive name (e.g., "My Company NuGet Feed")

**Supported account types:** Select one of:
- **Single tenant** (Recommended) - Only users in your organization
- **Multitenant** - Users from any Azure AD tenant
- **Multitenant + personal accounts** - Any Microsoft account

**Redirect URI:**
- Platform: **Web**
- URI: `https://localhost:7000/signin-oidc` (for development)

Click **Register**.

### Step 3: Note Application IDs

After registration, note these values (you'll need them later):

- **Application (client) ID** - Found on the Overview page
- **Directory (tenant) ID** - Found on the Overview page

### Step 4: Configure Authentication

1. Go to **Authentication** in the left menu
2. Under **Implicit grant and hybrid flows**, enable:
   - ✅ **Access tokens**
   - ✅ **ID tokens**
3. Click **Save**

### Step 5: Add Production Redirect URI

After deploying to production, add the production redirect URI:

1. Go to **Authentication**
2. Click **Add URI** under **Redirect URIs**
3. Add: `https://your-feed.azurewebsites.net/signin-oidc`
4. Click **Save**

## Configuring Your Application

### Update Configuration

Update `appsettings.Development.json` (or use template parameters):

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourcompany.com",
    "TenantId": "00000000-0000-0000-0000-000000000000",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "CallbackPath": "/signin-oidc"
  }
}
```

Replace:
- `yourcompany.com` with your Azure AD domain
- `00000000-...` with your Tenant ID
- `11111111-...` with your Client ID

### Using User Secrets (Recommended for Development)

```bash
dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
dotnet user-secrets set "AzureAd:Domain" "yourcompany.com"
```

### Production Configuration

For production, use:

**Azure App Service:**
```bash
az webapp config appsettings set \
  --name my-nuget-feed \
  --resource-group my-resource-group \
  --settings \
    AzureAd__TenantId="your-tenant-id" \
    AzureAd__ClientId="your-client-id" \
    AzureAd__Domain="yourcompany.com"
```

**Environment Variables:**
```bash
export AzureAd__TenantId="your-tenant-id"
export AzureAd__ClientId="your-client-id"
export AzureAd__Domain="yourcompany.com"
```

## How Authentication Works

### User Authentication Flow

1. User navigates to feed URL
2. Application redirects to Azure AD login page
3. User enters credentials (and completes MFA if required)
4. Azure AD validates credentials
5. Azure AD redirects to callback URL with authentication token
6. Application validates token and creates session
7. Application creates user account in database (if first time)
8. User is granted access to the application

### Account Selection

The template is configured to always prompt for account selection:

```csharp
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme, 
    options =>
{
    options.Prompt = "select_account";
});
```

This improves security by:
- Preventing accidental wrong account usage
- Supporting users with multiple organizational accounts
- Making it clear which account is being used

### Token Validation Event

When a user successfully authenticates, the `OnTokenValidated` event creates or updates their user record:

```csharp
static async Task OnTokenValidated(TokenValidatedContext ctx)
{
    var feedContext = ctx.HttpContext.RequestServices
        .GetRequiredService<FeedContext>();
    var email = ctx.Principal.FindFirstValue("preferred_username");
    var user = await feedContext.Users
        .FirstOrDefaultAsync(x => x.Email == email);
    
    if (user is null)
    {
        user = new User
        {
            Email = email,
            Name = ctx.Principal.FindFirstValue("name"),
            PackagePublisher = !await feedContext.Users.AnyAsync()
        };
        feedContext.Users.Add(user);
        await feedContext.SaveChangesAsync();
    }

    if (user.PackagePublisher)
    {
        var claimsIdentity = ctx.Principal.Identity as ClaimsIdentity;
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
    }
}
```

## Advanced Configuration

### Custom Claims

Access Azure AD claims in your application:

```csharp
var email = User.FindFirstValue("preferred_username");
var name = User.FindFirstValue("name");
var objectId = User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
var tenantId = User.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");
```

### Group-Based Authorization

Use Azure AD groups for role assignment:

1. **Add groups to token:**
   - In Azure Portal, go to **Token configuration**
   - Click **Add groups claim**
   - Select group types to include

2. **Check group membership:**
   ```csharp
   if (User.HasClaim("groups", "group-id-for-publishers"))
   {
       user.PackagePublisher = true;
   }
   ```

### Custom Domain

If you have a custom domain in Azure AD:

1. Set up custom domain in Azure AD
2. Update `Domain` in configuration
3. Users will sign in with `user@yourdomain.com` instead of `user@tenant.onmicrosoft.com`

## Multi-Tenant Scenarios

To support users from multiple Azure AD tenants:

1. **Configure as multi-tenant** in Azure AD app registration

2. **Update configuration:**
   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "TenantId": "common",
       "ClientId": "your-client-id",
       "CallbackPath": "/signin-oidc"
     }
   }
   ```

3. **Validate tenants** in `OnTokenValidated`:
   ```csharp
   var allowedTenants = new[] { "tenant1-id", "tenant2-id" };
   var tenantId = ctx.Principal.FindFirstValue("tid");
   
   if (!allowedTenants.Contains(tenantId))
   {
       ctx.Fail("Unauthorized tenant");
       return;
   }
   ```

## Troubleshooting

### "AADSTS50011: Reply URL mismatch"

**Cause:** Redirect URI not configured correctly in Azure AD.

**Solution:**
1. Check the redirect URI in Azure AD matches your application
2. Ensure protocol (http/https) matches
3. No trailing slash should be present

### "AADSTS700016: Application not found"

**Cause:** Client ID is incorrect or app doesn't exist in tenant.

**Solution:**
1. Verify the Client ID in your configuration
2. Ensure you're signed into the correct Azure AD tenant
3. Check the application hasn't been deleted

### "AADSTS50105: User not assigned"

**Cause:** User assignment is required but user isn't assigned to app.

**Solution:**
1. In Azure Portal, go to **Enterprise applications**
2. Find your application
3. Go to **Users and groups**
4. Click **Add user/group**
5. Assign the user to the application

Or disable user assignment requirement:
1. Go to **Enterprise applications** → Your app
2. Select **Properties**
3. Set **User assignment required?** to **No**

### "AADSTS65001: Consent required"

**Cause:** Admin consent is required but not granted.

**Solution:**
1. In Azure Portal, go to **API permissions**
2. Click **Grant admin consent for [Your Organization]**
3. Confirm the consent

### Users Can't Sign In

**Verify:**
1. Tenant ID is correct
2. Client ID is correct
3. Redirect URI matches exactly (including port for localhost)
4. ID tokens are enabled in Azure AD
5. User exists in the tenant

## Security Best Practices

1. **Use HTTPS only** - Never use HTTP in production
2. **Enable MFA** - Require multi-factor authentication for all users
3. **Regular review** - Audit app permissions and user assignments regularly
4. **Principle of least privilege** - Only grant necessary permissions
5. **Monitor sign-ins** - Review Azure AD sign-in logs for suspicious activity
6. **Rotate secrets** - If using client secrets, rotate them regularly
7. **Keep updated** - Update Microsoft.Identity.Web packages regularly

## Alternative Identity Providers

While this template uses Azure AD, you can integrate other providers:

### IdentityServer4/Duende IdentityServer

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
    options.ResponseType = "code";
    options.SaveTokens = true;
});
```

### Okta

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Okta";
})
.AddCookie()
.AddOktaMvc(new OktaMvcOptions
{
    OktaDomain = "https://your-domain.okta.com",
    ClientId = "your-client-id",
    ClientSecret = "your-client-secret"
});
```

### Auth0

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Auth0";
})
.AddCookie()
.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = "your-domain.auth0.com";
    options.ClientId = "your-client-id";
});
```

## Next Steps

- [Deploy to Azure App Service](azure.md)
- [Configure Storage Options](storage.md)
- [Set up User Management](../features/user-management.md)
