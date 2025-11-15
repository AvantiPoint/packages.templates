# Email Notifications

The template includes a comprehensive email notification system that automatically sends emails to users for important package feed events.

## Overview

Email notifications keep users informed about:

- **Security events** - New IP address access, token changes
- **Package events** - Upload confirmations, download notifications
- **Account events** - Welcome messages, token management

All emails are sent asynchronously and use customizable HTML templates.

## Supported Email Providers

The template supports three email providers:

### SendGrid

[SendGrid](https://sendgrid.com/) is a popular email delivery service.

**Configuration:**

```json
{
  "Email": {
    "Enabled": true,
    "FromAddress": "noreply@yourcompany.com",
    "FromName": "NuGet Feed",
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "SG.your-api-key-here"
    }
  }
}
```

**Setup:**
1. Sign up at [sendgrid.com](https://sendgrid.com/)
2. Create an API key with "Mail Send" permissions
3. Add the API key to your configuration

### Postmark

[Postmark](https://postmarkapp.com/) is a transactional email service.

**Configuration:**

```json
{
  "Email": {
    "Enabled": true,
    "FromAddress": "noreply@yourcompany.com",
    "FromName": "NuGet Feed",
    "Provider": "Postmark",
    "Postmark": {
      "ApiKey": "your-postmark-api-key"
    }
  }
}
```

**Setup:**
1. Sign up at [postmarkapp.com](https://postmarkapp.com/)
2. Create a server and get the API token
3. Add the API token to your configuration

### None (Disabled)

To disable email notifications:

```json
{
  "Email": {
    "Enabled": false,
    "Provider": "None"
  }
}
```

## Email Events

### First Token Created

Sent when a user creates their first API key.

**Trigger:** First API key creation

**Template:** `token-first-created`

**Content:**
- Welcome message
- Instructions for using the API key
- Link to documentation
- Security best practices

### Token Created

Sent when a user creates a new API key.

**Trigger:** Additional API key creation

**Template:** `token-created`

**Content:**
- Token description/name
- Creation timestamp
- Security reminder

### Token Revoked

Sent when a user revokes an API key.

**Trigger:** API key revocation

**Template:** `token-revoked`

**Content:**
- Token description/name
- Revocation timestamp
- Affected token identifier

### Package Uploaded

Sent when a package is successfully uploaded.

**Trigger:** Package publish via `dotnet nuget push`

**Template:** `package-uploaded`

**Content:**
- Package ID and version
- Upload timestamp
- API key used (by description)
- User agent and IP address

### Symbols Uploaded

Sent when debug symbols are uploaded.

**Trigger:** Symbols package publish (.snupkg)

**Template:** `symbols-uploaded`

**Content:**
- Package ID and version
- Upload timestamp
- API key used
- User agent and IP address

### First Use from New IP

Sent when a package is downloaded from a previously unseen IP address.

**Trigger:** First package download from a new IP

**Template:** `token-first-use`

**Content:**
- Security alert message
- IP address that accessed the feed
- Package that was downloaded
- API key used
- Timestamp

This helps detect potentially compromised API keys.

## Email Templates

Email templates use the [Handlebars](https://handlebarsjs.com/) template engine for dynamic content.

### Template Location

Templates are embedded resources in `Services/EmailTemplates.cs`:

```csharp
public static class EmailTemplates
{
    public const string TokenCreated = "token-created";
    public const string TokenRevoked = "token-revoked";
    public const string TokenFirstCreated = "token-first-created";
    public const string PackageUploaded = "package-uploaded";
    public const string SymbolsUploaded = "symbols-uploaded";
    public const string TokenFirstUse = "token-first-use";
}
```

### Template Variables

Templates have access to context variables:

**Package Actions:**
```csharp
{
    Id: "MyPackage",
    Version: "1.0.0",
    IPAddress: "192.168.1.100",
    TokenDescription: "Build Server",
    UserAgent: "NuGet Client/6.0.0"
}
```

**User Information** (from claims):
- `User.Name` - User's display name
- `User.Email` - User's email address
- `FeedName` - Name of the feed

### Customizing Templates

To customize email templates:

1. Locate the template resource file
2. Modify the HTML/Handlebars template
3. Rebuild the application

Example template:

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .header { background-color: #0078d4; color: white; padding: 20px; }
        .content { padding: 20px; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Package Uploaded</h1>
    </div>
    <div class="content">
        <p>Hello {{User.Name}},</p>
        <p>Your package <strong>{{Id}} v{{Version}}</strong> was successfully uploaded to the feed.</p>
        <ul>
            <li>Time: {{Timestamp}}</li>
            <li>API Key: {{TokenDescription}}</li>
            <li>IP Address: {{IPAddress}}</li>
        </ul>
        <p>Thank you for using our NuGet feed!</p>
    </div>
</body>
</html>
```

## Email Service Implementation

The email service is implemented in the `Services` folder:

### IEmailService Interface

```csharp
public interface IEmailService
{
    Task SendEmail(string templateId, MailAddress to, string subject, object context);
}
```

### Base Email Service

`BaseEmailService.cs` provides common functionality:

- Template rendering with Handlebars
- Template resource loading
- HTML generation

### Provider-Specific Services

Each provider has its own implementation:

- `SendGridEmailService.cs` - SendGrid integration
- `PostmarkEmailService.cs` - Postmark integration  
- `NullEmailService.cs` - No-op for disabled emails

### Service Registration

Email services are registered in `ServiceRegistrationExtensions.cs`:

```csharp
public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration)
{
    var settings = configuration.GetSection("Email").Get<EmailSettings>();
    
    if (!settings.Enabled)
    {
        services.AddSingleton<IEmailService, NullEmailService>();
        return services;
    }
    
    switch (settings.Provider)
    {
        case "SendGrid":
            services.AddSingleton<IEmailService, SendGridEmailService>();
            break;
        case "Postmark":
            services.AddSingleton<IEmailService, PostmarkEmailService>();
            break;
        default:
            services.AddSingleton<IEmailService, NullEmailService>();
            break;
    }
    
    return services;
}
```

## Triggering Email Notifications

Emails are triggered by `NuGetFeedActionHandler.cs`:

### Package Upload

```csharp
public async Task OnPackageUploaded(string packageId, string version)
{
    _logger.LogInformation($"{User.Identity.Name} uploaded {packageId}.{version}.nupkg");
    await SendEmail(EmailTemplates.PackageUploaded, 
        $"Package Uploaded - {packageId} {version}", 
        packageId, version);
}
```

### Package Download (New IP)

```csharp
public async Task OnPackageDownloaded(string packageId, string version)
{
    _logger.LogInformation($"{User.Identity.Name} downloaded {packageId}.{version}.nupkg");

    if (await _context.PackageDownloads.CountAsync(x => x.RemoteIp == RemoteIp) == 1)
        await SendEmail(EmailTemplates.TokenFirstUse, 
            "Token used from new IP Address", 
            packageId, version);
}
```

## Customizing Email Logic

### Adding New Email Types

1. Define a new template constant:

```csharp
public const string PackageDeleted = "package-deleted";
```

2. Create the email template HTML

3. Trigger the email from your handler:

```csharp
public async Task OnPackageDeleted(string packageId, string version)
{
    await SendEmail(EmailTemplates.PackageDeleted,
        $"Package Deleted - {packageId} {version}",
        packageId, version);
}
```

### Conditional Email Sending

Send emails based on conditions:

```csharp
public async Task OnPackageUploaded(string packageId, string version)
{
    // Only send email for production packages
    if (!packageId.EndsWith(".Dev"))
    {
        await SendEmail(EmailTemplates.PackageUploaded, 
            $"Package Uploaded - {packageId} {version}", 
            packageId, version);
    }
}
```

### Adding Recipients

Send emails to additional recipients:

```csharp
private async Task SendEmailToAdmins(string subject, object context)
{
    var admins = await _context.Users
        .Where(x => x.PackagePublisher)
        .ToListAsync();
    
    foreach (var admin in admins)
    {
        var to = new MailAddress(admin.Email, admin.Name);
        await _emailService.SendEmail("admin-notification", to, subject, context);
    }
}
```

## Using Custom Email Providers

To add support for a custom email provider:

1. **Create a new service class:**

```csharp
public class CustomEmailService : BaseEmailService
{
    private readonly CustomEmailClient _client;
    
    public CustomEmailService(EmailSettings settings, ITemplateResourceProvider templates)
        : base(templates)
    {
        _client = new CustomEmailClient(settings.CustomProvider.ApiKey);
    }
    
    public override async Task SendEmail(string templateId, MailAddress to, 
        string subject, object context)
    {
        var html = await RenderTemplate(templateId, context);
        
        await _client.SendAsync(new CustomEmailMessage
        {
            To = to.Address,
            Subject = subject,
            HtmlBody = html,
            From = _settings.FromAddress
        });
    }
}
```

2. **Add configuration:**

```json
{
  "Email": {
    "Provider": "Custom",
    "CustomProvider": {
      "ApiKey": "your-api-key"
    }
  }
}
```

3. **Register the service:**

```csharp
case "Custom":
    services.AddSingleton<IEmailService, CustomEmailService>();
    break;
```

## Testing Email Templates

To test emails without sending:

1. Use the `NullEmailService` during development
2. Add logging to see what would be sent:

```csharp
public class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;
    
    public async Task SendEmail(string templateId, MailAddress to, 
        string subject, object context)
    {
        _logger.LogInformation(
            $"Would send email: {subject} to {to.Address} using template {templateId}");
        await Task.CompletedTask;
    }
}
```

3. Write the rendered HTML to disk for review:

```csharp
var html = await RenderTemplate(templateId, context);
await File.WriteAllTextAsync($"/tmp/email-{templateId}.html", html);
```

## Best Practices

1. **Keep emails concise** - Include only essential information
2. **Use responsive design** - Ensure emails look good on mobile devices
3. **Test thoroughly** - Test emails with different providers and clients
4. **Monitor delivery** - Check provider dashboards for delivery issues
5. **Respect privacy** - Don't include sensitive information in emails
6. **Provide unsubscribe** - Allow users to opt out of non-critical emails
7. **Use clear subjects** - Make email purpose obvious from subject line

## Troubleshooting

### Emails Not Sending

**Check:**
- Email provider is configured correctly
- API key is valid
- `Enabled` is set to `true`
- Application logs for errors

### Wrong Sender Address

**Verify:**
- `FromAddress` is configured
- Sender domain is verified with email provider
- SPF/DKIM records are set up (for deliverability)

### Emails Going to Spam

**Improve deliverability:**
- Verify sender domain with email provider
- Set up SPF, DKIM, and DMARC records
- Use a dedicated sending domain
- Avoid spam trigger words in content

## Next Steps

- [Configure Package Syndication](package-syndication.md)
- [Manage Users](user-management.md)
- [Customize Templates](../advanced/customization.md)
