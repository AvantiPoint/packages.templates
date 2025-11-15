# Installation

This guide will walk you through installing the AvantiPoint Packages Template and creating your first NuGet feed.

## Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full version)
- A code editor (Visual Studio, VS Code, or Rider)
- (Optional) [Azure Account](https://azure.microsoft.com/free/) for Azure AD and blob storage

## Installing the Template

The template is distributed as a NuGet package. Install it using the .NET CLI:

```bash
dotnet new install AvantiPoint.Packages.Templates
```

### Verify Installation

After installation, you can verify the template is available:

```bash
dotnet new list
```

You should see `packagefeed` in the list of available templates:

```
Template Name          Short Name      Language  Tags
---------------------  --------------  --------  ----------------------
AvantiPoint.Packages   packagefeed     [C#]      aspnetcore/nuget
Template
```

## Creating a New Feed

Create a new NuGet feed project using the template:

```bash
dotnet new packagefeed -n MyNuGetFeed
```

### Template Parameters

You can customize the generated project by providing parameters during creation:

```bash
dotnet new packagefeed -n MyNuGetFeed \
  --ADDomain "mycompany.com" \
  --ADTenantId "your-tenant-id" \
  --ADClientId "your-client-id" \
  --SendGridApiKey "your-sendgrid-key"
```

Available parameters:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ADDomain` | Your Azure AD domain | `contoso.com` |
| `ADTenantId` | Azure AD Tenant ID | `00000000-0000-0000-0000-000000000000` |
| `ADClientId` | Azure AD Application Client ID | `11111111-1111-1111-1111-111111111111` |
| `SendGridApiKey` | SendGrid API key for emails | (empty) |
| `PostmarkApiKey` | Postmark API key for emails | (empty) |

!!! tip
    You can update these values later in `appsettings.json` or use [User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) for sensitive data.

## Project Structure

After creation, your project will have the following structure:

```
MyNuGetFeed/
├── Authentication/          # Authentication services
│   ├── PackageAuthenticationService.cs
│   ├── FeedClaims.cs
│   └── FeedRoles.cs
├── Configuration/           # Configuration models
│   ├── EmailSettings.cs
│   └── FeedSettings.cs
├── Controllers/             # API controllers
├── Data/                    # Entity Framework context and models
│   ├── FeedContext.cs
│   └── Models/
├── Migrations/              # Database migrations
├── Pages/                   # Razor Pages for web UI
│   ├── Account/            # User account management
│   ├── Packages/           # Package browsing
│   └── Profile/            # User profile
├── Services/                # Business logic services
│   ├── NuGetFeedActionHandler.cs
│   ├── EmailService.cs
│   └── SyndicationService.cs
├── wwwroot/                 # Static files
├── Program.cs               # Application entry point
├── appsettings.json         # Configuration
└── MyNuGetFeed.csproj      # Project file
```

## Next Steps

Now that you have created your feed project:

1. [Configure your feed](configuration.md) with Azure AD and email settings
2. [Run your feed locally](quick-start.md) to test it out
3. [Deploy to Azure](../hosting/azure.md) for production use

## Updating the Template

To update to the latest version of the template:

```bash
# Uninstall the old version
dotnet new uninstall AvantiPoint.Packages.Templates

# Install the latest version
dotnet new install AvantiPoint.Packages.Templates
```

## Troubleshooting

### Template Not Found

If the template doesn't appear after installation:

1. Clear the template cache: `dotnet new --debug:reinit`
2. Reinstall the template
3. Verify with `dotnet new list`

### Installation Errors

If you encounter errors during installation:

- Ensure you have the latest .NET SDK installed
- Check your NuGet sources are accessible
- Try installing with the `--force` flag: `dotnet new install AvantiPoint.Packages.Templates --force`

For more help, see the [Troubleshooting Guide](../advanced/troubleshooting.md) or open an issue on [GitHub](https://github.com/AvantiPoint/packages.templates/issues).
