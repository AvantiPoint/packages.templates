# Customization

This guide covers how to customize the NuGet feed template to meet your specific requirements.

## Overview

The template is designed to be customizable through several extensibility points:

- Custom authentication logic
- Custom event handlers
- Custom email providers
- Custom UI themes
- Additional features and workflows

## Customizing Authentication

### Custom Claims

Add custom claims during authentication in `Program.cs`:

```csharp
static async Task OnTokenValidated(TokenValidatedContext ctx)
{
    var feedContext = ctx.HttpContext.RequestServices.GetRequiredService<FeedContext>();
    var email = ctx.Principal.FindFirstValue("preferred_username");
    var user = await feedContext.Users.FirstOrDefaultAsync(x => x.Email == email);
    
    // ... existing user creation logic ...
    
    // Add custom claims
    var claimsIdentity = ctx.Principal.Identity as ClaimsIdentity;
    claimsIdentity.AddClaim(new Claim("Department", user.Department));
    claimsIdentity.AddClaim(new Claim("Location", user.Location));
}
```

### Custom Authorization Rules

Extend `PackageAuthenticationService.cs` for custom authorization:

```csharp
public class PackageAuthenticationService : IPackageAuthenticationService
{
    // ... existing implementation ...
    
    public async Task<bool> CanAccessPackage(string packageId, ClaimsPrincipal user)
    {
        // Custom logic based on package ID and user claims
        if (packageId.StartsWith("Internal."))
        {
            return user.HasClaim("Department", "Engineering");
        }
        
        return true;
    }
}
```

## Customizing Package Events

### Custom Event Handler

Modify `NuGetFeedActionHandler.cs` to add custom logic:

```csharp
public class NuGetFeedActionHandler : INuGetFeedActionHandler
{
    // ... existing properties and constructor ...
    
    public async Task OnPackageUploaded(string packageId, string version)
    {
        _logger.LogInformation($"{User.Identity.Name} uploaded {packageId}.{version}.nupkg");
        
        // Send email notification
        await SendEmail(EmailTemplates.PackageUploaded, 
            $"Package Uploaded - {packageId} {version}", 
            packageId, version);

        // Custom: Validate package metadata
        await ValidatePackageMetadata(packageId, version);
        
        // Custom: Notify external system via webhook
        await NotifyWebhook("package.uploaded", new { packageId, version });
        
        // Custom: Auto-approve for certain package prefixes
        if (packageId.StartsWith("Approved."))
        {
            await AutoApprovePackage(packageId, version);
        }
        
        // Syndicate to configured targets
        await _syndicationService.SyndicatePackage(packageId, NuGetVersion.Parse(version));
    }
    
    private async Task ValidatePackageMetadata(string packageId, string version)
    {
        // Your custom validation logic
    }
    
    private async Task NotifyWebhook(string eventType, object data)
    {
        // Your webhook notification logic
    }
}
```

### Adding New Events

Create new event methods:

```csharp
public async Task OnPackageDeleted(string packageId, string version)
{
    _logger.LogWarning($"{User.Identity.Name} deleted {packageId}.{version}");
    
    // Send notification
    await SendEmail("package-deleted", $"Package Deleted - {packageId} {version}", 
        packageId, version);
    
    // Audit log
    await _auditService.LogDeletion(packageId, version, User);
}
```

## Customizing Email Templates

### Modifying Existing Templates

Update templates in `Services/EmailTemplates.cs` (stored as embedded resources):

1. Locate the template HTML file
2. Modify using Handlebars syntax
3. Rebuild the application

Example template:

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
        .header { background-color: #0078d4; color: white; padding: 20px; }
        .content { padding: 20px; }
        .footer { padding: 20px; font-size: 12px; color: #666; }
    </style>
</head>
<body>
    <div class="header">
        <h1>{{Title}}</h1>
    </div>
    <div class="content">
        <p>Hello {{User.Name}},</p>
        <p>{{Message}}</p>
        <ul>
            <li>Package: <strong>{{Id}}</strong></li>
            <li>Version: <strong>{{Version}}</strong></li>
            <li>Time: {{Timestamp}}</li>
        </ul>
    </div>
    <div class="footer">
        <p>This is an automated message from {{FeedName}}.</p>
    </div>
</body>
</html>
```

### Adding New Email Templates

1. Create new template file in resources
2. Add template ID to `EmailTemplates.cs`:
   ```csharp
   public const string CustomEvent = "custom-event";
   ```
3. Trigger from your event handler:
   ```csharp
   await SendEmail(EmailTemplates.CustomEvent, "Subject", packageId, version);
   ```

## Customizing the Web UI

### Modifying Razor Pages

Update pages in the `Pages/` directory:

**Customize the homepage (`Pages/Index.cshtml`):**

```html
@page
@model IndexModel
@{
    ViewData["Title"] = "My Company NuGet Feed";
}

<div class="text-center">
    <h1 class="display-4">Welcome to Our NuGet Feed</h1>
    <p>Your internal package repository for .NET libraries.</p>
    
    @if (User.Identity.IsAuthenticated)
    {
        <a asp-page="/Packages/Index" class="btn btn-primary">Browse Packages</a>
        <a asp-page="/Account/ApiKeys" class="btn btn-secondary">Manage API Keys</a>
    }
    else
    {
        <a asp-area="MicrosoftIdentity" asp-controller="Account" asp-action="SignIn" 
           class="btn btn-primary">Sign In</a>
    }
</div>
```

### Custom Styling

Modify `wwwroot/css/site.css`:

```css
:root {
    --primary-color: #0078d4;
    --secondary-color: #106ebe;
    --success-color: #107c10;
    --danger-color: #d13438;
}

.navbar-brand {
    font-weight: bold;
    color: var(--primary-color) !important;
}

.btn-primary {
    background-color: var(--primary-color);
    border-color: var(--primary-color);
}

.btn-primary:hover {
    background-color: var(--secondary-color);
    border-color: var(--secondary-color);
}
```

### Adding Company Logo

1. Add logo to `wwwroot/images/logo.png`
2. Update `Pages/Shared/_Layout.cshtml`:

```html
<nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
    <div class="container">
        <a class="navbar-brand" asp-area="" asp-page="/Index">
            <img src="~/images/logo.png" alt="Logo" height="30" />
            My Company NuGet
        </a>
        <!-- ... rest of navbar ... -->
    </div>
</nav>
```

## Adding Custom Features

### Package Approval Workflow

Add approval requirement for packages:

1. Add `Approved` field to package model
2. Create approval page for Publishers
3. Modify `OnPackageUploaded` to mark as pending:

```csharp
public async Task OnPackageUploaded(string packageId, string version)
{
    var package = await _context.Packages
        .FirstOrDefaultAsync(p => p.Id == packageId && p.Version == version);
    
    // Mark as pending approval
    package.Approved = false;
    package.ApprovalStatus = "Pending";
    await _context.SaveChangesAsync();
    
    // Notify approvers
    await NotifyApprovers(packageId, version);
}
```

### Package Download Statistics

Track detailed download statistics:

```csharp
public async Task OnPackageDownloaded(string packageId, string version)
{
    // Record download
    var download = new PackageDownloadRecord
    {
        PackageId = packageId,
        Version = version,
        UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        IPAddress = Connection.RemoteIpAddress.ToString(),
        UserAgent = Request.Headers["User-Agent"],
        DownloadedAt = DateTime.UtcNow
    };
    
    _context.Downloads.Add(download);
    await _context.SaveChangesAsync();
    
    // Update package statistics
    await UpdatePackageStats(packageId);
}
```

### Integration with External Systems

#### Webhook Notifications

```csharp
private async Task SendWebhook(string eventType, object payload)
{
    var webhookUrl = _configuration["Webhooks:Url"];
    if (string.IsNullOrEmpty(webhookUrl)) return;
    
    var webhook = new
    {
        Event = eventType,
        Timestamp = DateTime.UtcNow,
        Data = payload
    };
    
    using var client = new HttpClient();
    await client.PostAsJsonAsync(webhookUrl, webhook);
}
```

#### Slack Notifications

```csharp
private async Task NotifySlack(string message)
{
    var webhookUrl = _configuration["Slack:WebhookUrl"];
    if (string.IsNullOrEmpty(webhookUrl)) return;
    
    var payload = new
    {
        text = message,
        username = "NuGet Feed Bot",
        icon_emoji = ":package:"
    };
    
    using var client = new HttpClient();
    await client.PostAsJsonAsync(webhookUrl, payload);
}
```

## Database Customization

### Adding Custom Fields

1. Add properties to entity models in `Data/Models/`
2. Create migration:
   ```bash
   dotnet ef migrations add AddCustomFields
   ```
3. Apply migration:
   ```bash
   dotnet ef database update
   ```

### Custom Entities

Create new entities for custom features:

```csharp
public class PackageReview
{
    public int Id { get; set; }
    public string PackageId { get; set; }
    public string Version { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public virtual User User { get; set; }
}
```

Add to `FeedContext.cs`:

```csharp
public DbSet<PackageReview> PackageReviews { get; set; }
```

## Configuration Extensions

Add custom configuration sections:

```json
{
  "CustomFeatures": {
    "EnableApproval": true,
    "EnableReviews": true,
    "MaxPackageSize": 104857600,
    "AllowedPackagePrefixes": ["MyCompany.", "Internal."]
  }
}
```

Create configuration class:

```csharp
public class CustomFeatureSettings
{
    public bool EnableApproval { get; set; }
    public bool EnableReviews { get; set; }
    public long MaxPackageSize { get; set; }
    public List<string> AllowedPackagePrefixes { get; set; }
}
```

Register in `Program.cs`:

```csharp
builder.Services.Configure<CustomFeatureSettings>(
    builder.Configuration.GetSection("CustomFeatures"));
```

## Next Steps

- [Learn about Extensibility Points](extensibility.md)
- [Troubleshooting Guide](troubleshooting.md)
- [View Template Parameters](../reference/template-parameters.md)
