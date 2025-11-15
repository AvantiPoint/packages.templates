# AvantiPoint Packages Templates

[![NuGet](https://img.shields.io/nuget/v/AvantiPoint.Packages.Templates.svg)](https://www.nuget.org/packages/AvantiPoint.Packages.Templates/)
[![License](https://img.shields.io/github/license/AvantiPoint/packages.templates.svg)](LICENSE)

A production-ready .NET template for creating self-hosted NuGet package feeds with enterprise features built on [AvantiPoint.Packages](https://github.com/AvantiPoint/avantipoint.packages).

## Overview

This template generates a complete NuGet feed application with:

- **Azure Active Directory Integration** - Enterprise single sign-on
- **User Management** - Role-based access control with Publisher and Consumer roles
- **API Key Management** - Self-service API key creation and management
- **Email Notifications** - Automated alerts for package events and security
- **Package Syndication** - Mirror packages to other feeds automatically
- **Modern Web UI** - Clean interface for package browsing and management
- **Flexible Storage** - File system or Azure Blob Storage support
- **Comprehensive Security** - IP tracking, audit logging, and token management

## Quick Start

### Installation

Install the template from NuGet:

```bash
dotnet new install AvantiPoint.Packages.Templates
```

### Create a New Feed

```bash
# Basic creation
dotnet new packagefeed -n MyNuGetFeed

# With Azure AD and email configured
dotnet new packagefeed -n MyNuGetFeed \
  --ADDomain "mycompany.com" \
  --ADTenantId "your-tenant-id" \
  --ADClientId "your-client-id" \
  --SendGridApiKey "your-sendgrid-key"
```

### Run Locally

```bash
cd MyNuGetFeed
dotnet ef database update
dotnet run
```

Navigate to `https://localhost:7000` and sign in with your Azure AD account.

## Features

### 🔐 Authentication & Security

- **Azure AD Integration** - Single sign-on with organizational accounts
- **API Key Authentication** - Secure token-based access for NuGet clients
- **Role-Based Access** - Publisher and Consumer roles with appropriate permissions
- **IP Tracking** - Monitor package access and detect suspicious activity
- **Audit Logging** - Complete audit trail of all operations

### 👥 User Management

- **Self-Service Registration** - Users automatically registered on first sign-in
- **First User Privilege** - First user automatically gets Publisher role
- **API Key Management** - Users create and manage their own NuGet API keys
- **User Profiles** - View user information and activity

### 📧 Email Notifications

- **Welcome Emails** - Sent when users create their first API key
- **Token Alerts** - Notifications for token creation and revocation
- **Upload Confirmations** - Alerts when packages are uploaded
- **Security Alerts** - Notifications when packages accessed from new IPs
- **Multiple Providers** - Support for SendGrid and Postmark

### 🔄 Package Syndication

- **Multi-Feed Support** - Syndicate to multiple target feeds
- **Package Groups** - Organize packages with pattern matching
- **Selective Sync** - Choose which packages to syndicate
- **Symbol Support** - Syndicate debug symbols automatically

### 💾 Storage Options

- **File System** - Local disk or network storage for development
- **Azure Blob Storage** - Scalable cloud storage for production
- **Easy Migration** - Switch between storage backends

### 🌐 Web Interface

- **Package Browsing** - Search and view packages
- **Account Management** - Manage API keys and settings
- **Admin Console** - Configure syndication and users (Publishers only)

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- SQL Server (LocalDB, Express, or full version)
- Azure AD tenant (for authentication)
- (Optional) Azure subscription for blob storage

## Documentation

📚 **[Full Documentation](https://avantipoint.github.io/packages.templates/)**

- [Installation Guide](docs/getting-started/installation.md) - Install and create your first feed
- [Quick Start](docs/getting-started/quick-start.md) - Get up and running quickly
- [Configuration](docs/getting-started/configuration.md) - Configure all settings
- [Features Overview](docs/features/overview.md) - Learn about all features
- [Azure Deployment](docs/hosting/azure.md) - Deploy to production
- [Customization](docs/advanced/customization.md) - Extend and customize
- [Troubleshooting](docs/advanced/troubleshooting.md) - Common issues and solutions

## Configuration

### Azure Active Directory Setup

1. Create an Azure AD application in the [Azure Portal](https://portal.azure.com)
2. Configure redirect URI: `https://localhost:7000/signin-oidc`
3. Enable ID tokens in Authentication settings
4. Note your Tenant ID and Client ID

### Application Settings

Update `appsettings.Development.json`:

```json
{
  "AzureAd": {
    "Domain": "yourcompany.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  },
  "Email": {
    "Enabled": true,
    "Provider": "SendGrid",
    "FromAddress": "noreply@yourcompany.com",
    "SendGrid": {
      "ApiKey": "your-api-key"
    }
  }
}
```

For detailed configuration options, see the [Configuration Guide](docs/getting-started/configuration.md).

## Using Your Feed

### Configure NuGet Client

Add your feed as a NuGet source:

```bash
dotnet nuget add source https://localhost:7000/v3/index.json \
  --name MyCompanyFeed \
  --username your-email@yourcompany.com \
  --password your-api-key \
  --store-password-in-clear-text
```

### Push a Package

```bash
dotnet nuget push MyPackage.1.0.0.nupkg \
  --source MyCompanyFeed \
  --api-key your-api-key
```

### Restore Packages

```bash
dotnet restore --source MyCompanyFeed
```

## Deployment

### Azure App Service

Deploy to Azure App Service for production:

```bash
# Create resources
az group create --name nuget-feed-rg --location eastus
az appservice plan create --name nuget-plan --resource-group nuget-feed-rg --sku P1V2
az webapp create --name my-nuget-feed --resource-group nuget-feed-rg --plan nuget-plan

# Deploy application
dotnet publish -c Release
az webapp deployment source config-zip --name my-nuget-feed --resource-group nuget-feed-rg --src publish.zip
```

See the [Azure Deployment Guide](docs/hosting/azure.md) for detailed instructions.

## How It Differs from BaGet

While [BaGet](https://github.com/loic-sharma/BaGet) is an excellent NuGet server, this template provides additional enterprise features:

| Feature | BaGet | AvantiPoint Template |
|---------|-------|---------------------|
| NuGet V3 Protocol | ✅ | ✅ |
| Symbol Server | ✅ | ✅ |
| Authentication | Basic API Key | Azure AD + API Keys |
| User Management | ❌ | ✅ Full Web UI |
| Email Notifications | ❌ | ✅ Configurable |
| Package Syndication | ❌ | ✅ Multi-target |
| Role-Based Access | ❌ | ✅ Publisher/Consumer |
| IP Tracking | ❌ | ✅ Security alerts |
| Audit Logging | Basic | ✅ Comprehensive |

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

- **Documentation**: [https://avantipoint.github.io/packages.templates/](https://avantipoint.github.io/packages.templates/)
- **Issues**: [GitHub Issues](https://github.com/AvantiPoint/packages.templates/issues)
- **Main Library**: [AvantiPoint.Packages](https://github.com/AvantiPoint/avantipoint.packages)

## License

See the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built on top of [AvantiPoint.Packages](https://github.com/AvantiPoint/avantipoint.packages), which provides the core NuGet server implementation.