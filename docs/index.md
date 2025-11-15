# AvantiPoint Packages Templates

Welcome to the AvantiPoint Packages Templates documentation. This template provides a complete, production-ready NuGet package feed solution built on top of [AvantiPoint.Packages](https://github.com/AvantiPoint/avantipoint.packages).

## What is AvantiPoint Packages Template?

The AvantiPoint Packages Template is a dotnet template that generates a fully-functional, self-hosted NuGet package feed with the following features out of the box:

- **Azure Active Directory Integration** - Seamless authentication using Microsoft Identity
- **User Management** - Web-based interface for managing users and permissions
- **API Key Management** - Users can create and manage their own NuGet API keys
- **Email Notifications** - Automatic email notifications for package events
- **Package Syndication** - Support for syndicating packages to other feeds
- **Role-Based Access Control** - Separate consumer and publisher roles
- **Modern UI** - Clean, responsive web interface for feed management
- **Flexible Storage** - Support for file system or Azure Blob Storage

## Why Use This Template?

### Quick Setup

Get a production-ready NuGet feed running in minutes, not hours. The template includes all the infrastructure code you need:

- Authentication and authorization
- Database schema and Entity Framework migrations
- Email service integration (SendGrid and Postmark)
- Package upload and download handlers
- Web UI for user and package management

### Enterprise Features

Built for real-world use cases with features like:

- Multi-user support with role-based permissions
- Audit logging of all package operations
- IP-based access tracking
- Token revocation and management
- Email notifications for security events

### Built on AvantiPoint.Packages

This template leverages the [AvantiPoint.Packages](https://github.com/AvantiPoint/avantipoint.packages) library, which provides:

- Full NuGet V3 protocol implementation
- Package search and metadata APIs
- Symbol server support
- Vulnerability scanning integration (planned)
- High-performance package storage and retrieval

## How It Differs from BaGet

While [BaGet](https://github.com/loic-sharma/BaGet) is an excellent open-source NuGet server, AvantiPoint Packages and this template provide additional enterprise features:

| Feature | BaGet | AvantiPoint Template |
|---------|-------|---------------------|
| NuGet V3 Protocol | ✅ | ✅ |
| Symbol Server | ✅ | ✅ |
| Authentication | Basic API keys | Azure AD + API keys |
| User Management | ❌ | ✅ Web UI |
| Email Notifications | ❌ | ✅ Configurable |
| Multi-user Support | Limited | ✅ Full RBAC |
| Package Syndication | ❌ | ✅ |
| Audit Logging | ❌ | ✅ |
| IP Tracking | ❌ | ✅ |

## Getting Started

Ready to create your own NuGet feed? Head over to the [Installation Guide](getting-started/installation.md) to get started.

### Quick Install

```bash
# Install the template
dotnet new install AvantiPoint.Packages.Templates

# Create a new feed
dotnet new packagefeed -n MyNuGetFeed

# Navigate to the project
cd MyNuGetFeed

# Run the feed
dotnet run
```

## Documentation Structure

- **[Getting Started](getting-started/installation.md)** - Install and configure your first feed
- **[Features](features/overview.md)** - Deep dive into template features
- **[Hosting](hosting/azure.md)** - Deployment and hosting options
- **[Advanced](advanced/customization.md)** - Customization and extensibility
- **[Reference](reference/template-parameters.md)** - Complete reference documentation

## Support and Contributions

- **Issues**: [GitHub Issues](https://github.com/AvantiPoint/packages.templates/issues)
- **Source**: [GitHub Repository](https://github.com/AvantiPoint/packages.templates)
- **Main Library**: [AvantiPoint.Packages](https://github.com/AvantiPoint/avantipoint.packages)

## License

This template is provided under the same license as the AvantiPoint.Packages library. See the [LICENSE](https://github.com/AvantiPoint/packages.templates/blob/master/LICENSE) file for details.
