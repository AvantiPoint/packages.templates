# Configuration

This guide covers all configuration options available for your NuGet feed.

## Configuration Files

The template generates several configuration files:

- `appsettings.json` - Base configuration for all environments
- `appsettings.Development.json` - Development-specific settings (generated from template)
- `appsettings.Production.json` - Production settings (create manually)

## Azure AD Configuration

Configure Azure Active Directory authentication:

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

### Options

| Setting | Description | Required |
|---------|-------------|----------|
| `Instance` | Azure AD authentication endpoint | Yes |
| `Domain` | Your organization's Azure AD domain | Yes |
| `TenantId` | Azure AD tenant/directory ID | Yes |
| `ClientId` | Application (client) ID from Azure AD | Yes |
| `CallbackPath` | OAuth callback path | Yes |

!!! tip "Finding Your Values"
    - **Tenant ID**: Azure Portal → Azure Active Directory → Overview
    - **Client ID**: Azure Portal → App registrations → Your app → Overview
    - **Domain**: Your organization's primary domain (e.g., `contoso.com`)

## Database Configuration

Configure the SQL Server connection:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NuGetFeed;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Connection String Formats

**SQL Server LocalDB (Development):**
```
Server=(localdb)\\mssqllocaldb;Database=MyFeed;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server with Windows Authentication:**
```
Server=myserver;Database=MyFeed;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server with SQL Authentication:**
```
Server=myserver;Database=MyFeed;User Id=myuser;Password=mypassword;MultipleActiveResultSets=true
```

**Azure SQL Database:**
```
Server=tcp:myserver.database.windows.net,1433;Database=MyFeed;User ID=myuser;Password=mypassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Feed Configuration

Configure feed behavior and features:

```json
{
  "Feed": {
    "ServerName": "My Company NuGet Feed",
    "AllowAnonymousAccess": false,
    "PackageDeletionBehavior": "Unlist",
    "EnablePackageOverwrite": false
  }
}
```

### Options

| Setting | Description | Default |
|---------|-------------|---------|
| `ServerName` | Display name for your feed | Required |
| `AllowAnonymousAccess` | Allow unauthenticated package downloads | `false` |
| `PackageDeletionBehavior` | How to handle package deletion: `Unlist` or `HardDelete` | `Unlist` |
| `EnablePackageOverwrite` | Allow re-uploading packages with same version | `false` |

!!! warning
    Setting `AllowAnonymousAccess` to `true` allows anyone to download packages without authentication. Only enable this for public feeds.

## Storage Configuration

Choose between file system or Azure Blob Storage:

### File System Storage (Default)

```json
{
  "Storage": {
    "Type": "FileSystem",
    "Path": "Packages"
  }
}
```

### Azure Blob Storage

```json
{
  "Storage": {
    "Type": "AzureBlobStorage",
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net",
    "Container": "packages"
  }
}
```

!!! tip "When to Use Azure Blob Storage"
    Use Azure Blob Storage for:
    
    - Production deployments
    - High availability requirements
    - Multiple web server instances
    - Large package volumes
    - Automatic backup and geo-replication

## Email Configuration

Configure email notifications for package events:

```json
{
  "Email": {
    "Enabled": true,
    "FromAddress": "noreply@yourcompany.com",
    "FromName": "NuGet Feed Notifications",
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "SG.your-api-key-here"
    },
    "Postmark": {
      "ApiKey": "your-postmark-key-here"
    }
  }
}
```

### Options

| Setting | Description | Default |
|---------|-------------|---------|
| `Enabled` | Enable/disable email notifications | `true` |
| `FromAddress` | Sender email address | Required |
| `FromName` | Sender display name | Required |
| `Provider` | Email provider: `SendGrid`, `Postmark`, or `None` | `None` |

### Supported Email Providers

**SendGrid:**
1. Sign up at [sendgrid.com](https://sendgrid.com/)
2. Create an API key with "Mail Send" permissions
3. Add the API key to your configuration

**Postmark:**
1. Sign up at [postmarkapp.com](https://postmarkapp.com/)
2. Create a server and get the API token
3. Add the API token to your configuration

### Email Templates

Emails are sent for these events:

- **First Token Created** - Welcome email when user creates first API key
- **Token Created** - Confirmation when new API key is created
- **Token Revoked** - Notification when API key is revoked
- **Package Uploaded** - Confirmation of package upload
- **Symbols Uploaded** - Confirmation of symbols upload
- **New IP Access** - Security alert when package is downloaded from new IP

## Syndication Configuration

Configure package syndication to mirror packages to other feeds:

```json
{
  "Syndication": {
    "Enabled": true,
    "Targets": []
  }
}
```

Syndication targets are managed through the web UI under **Account** → **Publish Targets**.

## Logging Configuration

Configure application logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Log Levels

- `Trace` - Most verbose, includes all details
- `Debug` - Detailed debugging information
- `Information` - General informational messages
- `Warning` - Warning messages for non-critical issues
- `Error` - Error messages for failures
- `Critical` - Critical failures requiring immediate attention
- `None` - Disable logging

!!! tip "Production Logging"
    In production, consider:
    
    - Setting default log level to `Warning` or `Error`
    - Using Application Insights or similar service
    - Enabling structured logging with Serilog
    - Setting up log retention policies

## Environment-Specific Configuration

### User Secrets (Development)

For development, use User Secrets to store sensitive data:

```bash
# Initialize user secrets
dotnet user-secrets init

# Set values
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
dotnet user-secrets set "Email:SendGrid:ApiKey" "your-api-key"
```

### Environment Variables (Production)

In production, use environment variables:

```bash
# Format: ParentSection__ChildSection__Setting
export AzureAd__ClientId="your-client-id"
export Email__SendGrid__ApiKey="your-api-key"
export ConnectionStrings__DefaultConnection="your-connection-string"
```

### Azure App Service

When hosting on Azure App Service, configure in **Configuration** → **Application settings**:

```
AzureAd__ClientId = your-client-id
AzureAd__TenantId = your-tenant-id
Email__SendGrid__ApiKey = your-api-key
```

## Advanced Settings

### Request Size Limits

The template pre-configures large request limits for NuGet packages:

```csharp
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue;
});
```

These settings allow uploading large packages (100MB+).

### Database Migration

Apply migrations on application startup (configured by default):

```csharp
await app.InitializeDatabaseContext();
```

To disable automatic migration (recommended for production):

1. Comment out the line in `Program.cs`
2. Apply migrations manually before deployment:
   ```bash
   dotnet ef database update --connection "your-connection-string"
   ```

## Configuration Best Practices

1. **Never commit secrets** - Use User Secrets, Key Vault, or environment variables
2. **Use different settings per environment** - Separate dev, staging, and production configs
3. **Validate configuration** - Test settings before deploying to production
4. **Monitor logs** - Set appropriate log levels and monitor for errors
5. **Regular backups** - Back up your database and package storage
6. **Use HTTPS** - Always use HTTPS in production (enabled by default)
7. **Secure connection strings** - Use managed identities or Key Vault for database access

## Next Steps

- [Learn about Authentication](../features/authentication.md)
- [Configure Email Notifications](../features/email-notifications.md)
- [Set up Package Syndication](../features/package-syndication.md)
- [Deploy to Azure](../hosting/azure.md)
