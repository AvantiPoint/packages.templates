# Quick Start

This guide will help you get your NuGet feed up and running quickly for local development and testing.

## Before You Begin

Make sure you have:

1. [Installed the template](installation.md) and created a new project
2. SQL Server or SQL Server LocalDB installed
3. An Azure AD application created (see [Azure AD Integration](../hosting/azure-ad.md))

## Step 1: Configure Azure AD

Create an Azure AD application in the [Azure Portal](https://portal.azure.com):

1. Navigate to **Azure Active Directory** → **App registrations** → **New registration**
2. Set a name (e.g., "My NuGet Feed")
3. Set **Supported account types** to "Accounts in this organizational directory only"
4. Add a **Redirect URI**: `https://localhost:7000/signin-oidc` (Web platform)
5. Click **Register**

After registration:

1. Note the **Application (client) ID** and **Directory (tenant) ID**
2. Go to **Authentication** → **Implicit grant and hybrid flows**
3. Enable **ID tokens** and **Access tokens**
4. Click **Save**

## Step 2: Configure Your Feed

Update `appsettings.Development.json` with your Azure AD settings:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourcompany.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CallbackPath": "/signin-oidc"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyNuGetFeed;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Feed": {
    "ServerName": "My Company NuGet Feed",
    "AllowAnonymousAccess": false
  },
  "Email": {
    "Enabled": true,
    "FromAddress": "noreply@yourcompany.com",
    "FromName": "NuGet Feed",
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "your-sendgrid-api-key"
    }
  }
}
```

!!! warning "Keep Secrets Secure"
    For production, use [Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/), [User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets), or environment variables instead of storing sensitive values in `appsettings.json`.

## Step 3: Initialize the Database

Run Entity Framework migrations to create the database:

```bash
dotnet ef database update
```

This will create the necessary tables for:

- User accounts
- API tokens
- Package metadata
- Download tracking
- Package groups
- Syndication targets

## Step 4: Run the Feed

Start the application:

```bash
dotnet run
```

The feed will start on `https://localhost:7000` (or the port specified in `launchSettings.json`).

You should see output similar to:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## Step 5: Access the Web UI

Open your browser and navigate to `https://localhost:7000`.

You will be redirected to Azure AD to sign in. After signing in:

1. You'll be redirected back to the feed homepage
2. As the first user, you'll automatically have **Publisher** privileges
3. You can manage your API keys under the **Account** menu

## Step 6: Create an API Key

To upload or download packages, you need an API key:

1. Click **Account** → **API Keys** in the navigation menu
2. Click **Create New Token**
3. Enter a description (e.g., "Development Machine")
4. Click **Create**
5. **Copy the generated API key** - you won't see it again!

## Step 7: Configure NuGet Client

Add your feed as a NuGet source:

```bash
# Add the feed source
dotnet nuget add source https://localhost:7000/v3/index.json \
  --name MyCompanyFeed \
  --username your-email@yourcompany.com \
  --password your-api-key \
  --store-password-in-clear-text
```

!!! note
    Replace `your-email@yourcompany.com` with your Azure AD email and `your-api-key` with the key you created in Step 6.

## Step 8: Test Package Upload

Create a test package and upload it:

```bash
# Create a test library
dotnet new classlib -n TestPackage
cd TestPackage

# Pack the library
dotnet pack

# Push to your feed
dotnet nuget push bin/Debug/TestPackage.1.0.0.nupkg \
  --source MyCompanyFeed \
  --api-key your-api-key
```

You should see:

```
Pushing TestPackage.1.0.0.nupkg to 'https://localhost:7000/v3/index.json'...
  PUT https://localhost:7000/v3/index.json
  Created https://localhost:7000/v3/index.json 1234ms
Your package was pushed.
```

## Step 9: Search for Packages

Search for your package:

```bash
dotnet nuget search TestPackage --source MyCompanyFeed
```

Or browse packages in the web UI at `https://localhost:7000/Packages`.

## What's Next?

Now that you have a working feed, explore these topics:

- **[Configuration Options](configuration.md)** - Fine-tune your feed settings
- **[User Management](../features/user-management.md)** - Add more users and manage permissions
- **[Email Notifications](../features/email-notifications.md)** - Configure email alerts
- **[Package Syndication](../features/package-syndication.md)** - Mirror packages to other feeds
- **[Deploy to Azure](../hosting/azure.md)** - Host your feed in production

## Common Issues

### Database Connection Errors

If you get database connection errors:

1. Verify SQL Server LocalDB is installed: `sqllocaldb info`
2. Check the connection string in `appsettings.Development.json`
3. Ensure migrations have been applied: `dotnet ef database update`

### Azure AD Sign-In Issues

If sign-in fails:

1. Verify the Tenant ID and Client ID are correct
2. Check the redirect URI matches your configuration
3. Ensure ID tokens are enabled in Azure AD app settings
4. Clear browser cookies and try again

### Package Push Failures

If package push fails:

1. Verify your API key is valid and not revoked
2. Check you have Publisher role (first user is auto-assigned)
3. Ensure the feed URL is correct
4. Check firewall/antivirus isn't blocking the connection

For more troubleshooting help, see the [Troubleshooting Guide](../advanced/troubleshooting.md).
