# Features Overview

The AvantiPoint Packages Template provides a comprehensive set of features for hosting and managing your own NuGet package feed.

## Core Features

### 1. NuGet V3 Protocol Support

Full implementation of the NuGet V3 API protocol:

- **Package Search** - Fast, indexed search across all packages
- **Package Metadata** - Complete package metadata and versioning
- **Package Upload** - Push packages via `dotnet nuget push`
- **Package Download** - Download packages via `dotnet restore` or `dotnet add package`
- **Symbol Server** - Debug symbol hosting and source indexing
- **Package Deletion** - Unlist or hard-delete packages

### 2. Authentication & Authorization

Enterprise-grade security with Azure Active Directory:

- **Azure AD Integration** - Single sign-on with your organization
- **API Key Management** - User-managed API keys for NuGet operations
- **Role-Based Access Control** - Separate Publisher and Consumer roles
- **Token Lifecycle** - Create, manage, and revoke API tokens
- **IP Tracking** - Monitor package downloads by IP address

[Learn more about Authentication →](authentication.md)

### 3. User Management

Web-based user and permission management:

- **Self-Service Registration** - Users sign in and create their own API keys
- **Publisher Permissions** - Control who can upload packages
- **User Profiles** - View and manage user information
- **Activity Tracking** - Monitor user actions and downloads
- **First User Privilege** - First user automatically gets Publisher role

[Learn more about User Management →](user-management.md)

### 4. Email Notifications

Automated email alerts for important events:

- **Welcome Emails** - Sent when users create their first API key
- **Token Notifications** - Alerts for token creation and revocation
- **Package Upload Alerts** - Confirmation when packages are uploaded
- **Security Alerts** - Notification when packages are accessed from new IPs
- **Customizable Templates** - Handlebars-based email templates

[Learn more about Email Notifications →](email-notifications.md)

### 5. Package Syndication

Mirror packages to other feeds automatically:

- **Multi-Feed Support** - Syndicate to multiple target feeds
- **Selective Syndication** - Choose which packages to mirror
- **Package Groups** - Organize packages for syndication
- **Automatic Propagation** - New packages auto-sync to targets
- **Symbol Syndication** - Mirror debug symbols too

[Learn more about Package Syndication →](package-syndication.md)

## Storage Options

### File System Storage

Simple, reliable storage for development and small deployments:

- **Local File Storage** - Packages stored on local disk
- **Network Shares** - Support for UNC paths and mapped drives
- **Easy Backup** - Simple file-based backups
- **No Dependencies** - No external services required

### Azure Blob Storage

Cloud storage for production deployments:

- **Scalability** - Handle large volumes of packages
- **High Availability** - Built-in redundancy and failover
- **Geo-Replication** - Automatic data replication across regions
- **Cost-Effective** - Pay only for storage used
- **CDN Integration** - Optional CDN for faster downloads

[Learn more about Storage Options →](../hosting/storage.md)

## Web Interface

Modern, responsive web UI for feed management:

### Package Browsing
- View all packages and versions
- Search and filter packages
- Package detail pages with metadata
- Download statistics

### Account Management
- Create and manage API keys
- View token usage history
- Revoke tokens when needed
- Update profile information

### Administration (Publishers)
- Manage user permissions
- Configure syndication targets
- Organize package groups
- Monitor feed activity

## Database

SQL Server database for metadata and tracking:

- **User Accounts** - Store user profiles and permissions
- **API Tokens** - Secure API key storage with hashing
- **Package Metadata** - Index for fast package search
- **Download History** - Track package downloads by user and IP
- **Package Groups** - Organize packages for syndication
- **Syndication Targets** - Configured target feed endpoints

Entity Framework Core migrations included for easy schema updates.

## Security Features

### Authentication
- **Azure AD Integration** - Enterprise identity provider
- **Multi-Factor Authentication** - Leverages Azure AD MFA policies
- **API Key Authentication** - Secure token-based access for NuGet clients
- **Account Selection** - Prompt users to select account on login

### Authorization
- **Role-Based Access** - Publisher and Consumer roles
- **Token Scoping** - API keys tied to user accounts
- **Anonymous Access Control** - Optional anonymous package downloads
- **IP Tracking** - Monitor and alert on suspicious access patterns

### Data Protection
- **HTTPS Enforced** - All traffic encrypted in transit
- **Secure Token Storage** - API keys hashed in database
- **SQL Injection Protection** - Parameterized queries via Entity Framework
- **CSRF Protection** - Built-in anti-forgery tokens
- **XSS Protection** - Output encoding in Razor views

## Performance Features

### Caching
- **Package Metadata Cache** - Fast package search and listing
- **Static Content Cache** - Browser caching for UI assets
- **Database Connection Pooling** - Efficient database connections

### Optimization
- **Async Operations** - Non-blocking I/O throughout
- **Streaming Uploads** - Direct streaming to storage
- **Indexed Search** - Database indexes for fast queries
- **Request Size Limits** - Configured for large package uploads

## Monitoring & Logging

Built-in logging and diagnostics:

- **Structured Logging** - ASP.NET Core logging framework
- **Request Logging** - HTTP request/response tracking
- **Error Logging** - Exception tracking and details
- **Authentication Logging** - Login and token usage events
- **Performance Logging** - Slow query and operation tracking

Compatible with:
- Application Insights
- Serilog
- NLog
- Log4Net

## Extensibility Points

The template is designed for customization:

### Custom Authentication
- Implement `IPackageAuthenticationService` for custom auth
- Add additional authentication schemes
- Integrate with other identity providers

### Custom Event Handlers
- Implement `INuGetFeedActionHandler` for custom logic
- Hook into package upload/download events
- Add custom validation or workflow steps

### Custom Email Service
- Implement `IEmailService` for custom email providers
- Use your own email templates
- Add custom notification logic

### Custom Storage
- AvantiPoint.Packages supports custom storage providers
- Implement your own storage backend
- Use S3, Google Cloud Storage, or any custom solution

[Learn more about Customization →](../advanced/customization.md)

## Comparison with Other Solutions

### vs BaGet

| Feature | BaGet | AvantiPoint Template |
|---------|-------|---------------------|
| NuGet V3 Protocol | ✅ | ✅ |
| Symbol Server | ✅ | ✅ |
| File Storage | ✅ | ✅ |
| Cloud Storage | ✅ | ✅ Azure Blob |
| Authentication | Basic API Key | Azure AD + API Keys |
| User Management | Limited | ✅ Full Web UI |
| Email Notifications | ❌ | ✅ |
| Package Syndication | ❌ | ✅ |
| Role-Based Access | ❌ | ✅ |
| IP Tracking | ❌ | ✅ |
| Activity Logging | Basic | ✅ Comprehensive |

### vs Azure Artifacts

| Feature | Azure Artifacts | AvantiPoint Template |
|---------|-----------------|---------------------|
| Hosting | Cloud Only | Self-Hosted |
| Cost | Per-user pricing | Infrastructure only |
| Control | Limited | Full control |
| Customization | Limited | Fully customizable |
| Integration | Azure DevOps | Any CI/CD |
| On-Premises | ❌ | ✅ |

### vs MyGet

| Feature | MyGet | AvantiPoint Template |
|---------|-------|---------------------|
| Hosting | Cloud Only | Self-Hosted |
| Cost | Subscription | Free (self-hosted) |
| Private Feeds | ✅ | ✅ |
| Public Feeds | ✅ | Optional |
| Source Code | Closed | Open Source Template |

## Planned Features

The AvantiPoint.Packages library and template are actively developed. Upcoming features include:

- **Vulnerability Scanning** - Integration with NuGet vulnerability database
- **Package Signing Validation** - Verify signed packages
- **Retention Policies** - Automatic cleanup of old package versions
- **Package Mirroring** - Proxy packages from nuget.org
- **Advanced Search** - Full-text search with filters
- **Package Statistics** - Detailed download and usage analytics
- **Webhooks** - HTTP callbacks for package events
- **Multi-Tenancy** - Support multiple isolated feeds

## Next Steps

Explore the features in detail:

- [Authentication](authentication.md) - How authentication and authorization work
- [Email Notifications](email-notifications.md) - Configure and customize emails
- [Package Syndication](package-syndication.md) - Set up package mirroring
- [User Management](user-management.md) - Manage users and permissions
