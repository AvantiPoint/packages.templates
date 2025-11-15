# Extensibility

The AvantiPoint Packages Template provides several extensibility points for implementing custom functionality.

## Core Extensibility Interfaces

### IPackageAuthenticationService

Implement custom authentication logic for NuGet operations.

**Interface:**
```csharp
public interface IPackageAuthenticationService
{
    Task<NuGetAuthenticationResult> AuthenticateAsync(
        string apiKey, 
        CancellationToken cancellationToken);
        
    Task<NuGetAuthenticationResult> AuthenticateAsync(
        string username, 
        string token, 
        CancellationToken cancellationToken);
}
```

**Example Implementation:**

```csharp
public class CustomAuthenticationService : IPackageAuthenticationService
{
    public async Task<NuGetAuthenticationResult> AuthenticateAsync(
        string apiKey, 
        CancellationToken cancellationToken)
    {
        // Call external authentication service
        var result = await _externalAuthService.ValidateApiKeyAsync(apiKey);
        
        if (result.IsValid)
        {
            var identity = new ClaimsIdentity("Custom Auth");
            identity.AddClaim(new Claim(ClaimTypes.Name, result.UserName));
            identity.AddClaim(new Claim(ClaimTypes.Role, result.Role));
            
            return NuGetAuthenticationResult.Success(new ClaimsPrincipal(identity));
        }
        
        return NuGetAuthenticationResult.Fail("Invalid credentials");
    }
}
```

**Registration:**

```csharp
builder.Services.AddScoped<IPackageAuthenticationService, CustomAuthenticationService>();
```

### INuGetFeedActionHandler

Implement custom logic for package lifecycle events.

**Interface:**

```csharp
public interface INuGetFeedActionHandler
{
    Task<bool> CanDownloadPackage(string packageId, string version);
    Task OnPackageDownloaded(string packageId, string version);
    Task OnPackageUploaded(string packageId, string version);
    Task OnSymbolsDownloaded(string packageId, string version);
    Task OnSymbolsUploaded(string packageId, string version);
}
```

**Example Implementation:**

```csharp
public class CustomActionHandler : INuGetFeedActionHandler
{
    private readonly ILogger<CustomActionHandler> _logger;
    private readonly IMetricsService _metrics;
    private readonly IWebhookService _webhooks;
    
    public async Task<bool> CanDownloadPackage(string packageId, string version)
    {
        // Custom authorization logic
        var package = await _packageRepository.GetAsync(packageId, version);
        
        if (package.IsPrivate && !User.HasClaim("Department", package.OwningDepartment))
        {
            _logger.LogWarning($"Access denied to {packageId} for {User.Identity.Name}");
            return false;
        }
        
        return true;
    }
    
    public async Task OnPackageUploaded(string packageId, string version)
    {
        // Record metrics
        _metrics.IncrementCounter("packages.uploaded");
        
        // Send webhook
        await _webhooks.SendAsync("package.uploaded", new
        {
            PackageId = packageId,
            Version = version,
            UploadedBy = User.Identity.Name,
            Timestamp = DateTime.UtcNow
        });
        
        // Run security scan
        await _securityScanner.ScanPackageAsync(packageId, version);
    }
}
```

### IEmailService

Implement custom email providers or notification services.

**Interface:**

```csharp
public interface IEmailService
{
    Task SendEmail(string templateId, MailAddress to, string subject, object context);
}
```

**Example Implementation (Microsoft Graph):**

```csharp
public class GraphEmailService : IEmailService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ITemplateResourceProvider _templates;
    
    public async Task SendEmail(string templateId, MailAddress to, string subject, object context)
    {
        var html = await RenderTemplate(templateId, context);
        
        var message = new Message
        {
            Subject = subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = html
            },
            ToRecipients = new List<Recipient>
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = to.Address,
                        Name = to.DisplayName
                    }
                }
            }
        };
        
        await _graphClient.Users["sender@company.com"]
            .SendMail(message, true)
            .Request()
            .PostAsync();
    }
}
```

### ISyndicationService

Implement custom package syndication logic.

**Interface:**

```csharp
public interface ISyndicationService
{
    Task SyndicatePackage(string packageId, NuGetVersion version);
    Task SyndicateSymbols(string packageId, NuGetVersion version);
}
```

**Example Implementation:**

```csharp
public class CustomSyndicationService : ISyndicationService
{
    public async Task SyndicatePackage(string packageId, NuGetVersion version)
    {
        var targets = await GetSyndicationTargets(packageId);
        
        foreach (var target in targets)
        {
            try
            {
                // Custom logic: Validate package before syndicating
                if (!await ValidateForTarget(packageId, version, target))
                {
                    _logger.LogWarning($"Package {packageId} failed validation for {target.Name}");
                    continue;
                }
                
                // Custom logic: Transform package metadata
                var transformedPackage = await TransformPackage(packageId, version, target);
                
                // Push to target
                await PushToTarget(transformedPackage, target);
                
                // Custom logic: Notify on success
                await NotifySuccess(packageId, version, target);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to syndicate {packageId} to {target.Name}");
                await NotifyFailure(packageId, version, target, ex);
            }
        }
    }
}
```

## Custom Storage Providers

### Implementing Custom Storage

Implement the storage interface from AvantiPoint.Packages:

```csharp
public class CustomStorageProvider : IStorageProvider
{
    public async Task<Stream> GetPackageStreamAsync(string id, string version)
    {
        // Retrieve from custom storage
        return await _customStorage.GetObjectAsync($"{id}/{version}/{id}.{version}.nupkg");
    }
    
    public async Task SavePackageAsync(string id, string version, Stream packageStream)
    {
        // Save to custom storage
        await _customStorage.PutObjectAsync(
            $"{id}/{version}/{id}.{version}.nupkg", 
            packageStream);
    }
    
    public async Task DeletePackageAsync(string id, string version)
    {
        // Delete from custom storage
        await _customStorage.DeleteObjectAsync($"{id}/{version}");
    }
}
```

**Register the provider:**

```csharp
builder.Services.AddSingleton<IStorageProvider, CustomStorageProvider>();
```

## Custom Middleware

### Package Validation Middleware

```csharp
public class PackageValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PackageValidationMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate package uploads
        if (context.Request.Path.StartsWithSegments("/api/v2/package") 
            && context.Request.Method == "PUT")
        {
            // Validate package before processing
            if (!await ValidatePackageAsync(context.Request.Body))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Package validation failed");
                return;
            }
        }
        
        await _next(context);
    }
    
    private async Task<bool> ValidatePackageAsync(Stream packageStream)
    {
        // Custom validation logic
        return true;
    }
}
```

**Register middleware:**

```csharp
app.UseMiddleware<PackageValidationMiddleware>();
```

### Rate Limiting Middleware

```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var key = $"ratelimit_{context.Connection.RemoteIpAddress}";
        
        if (!_cache.TryGetValue(key, out int requestCount))
        {
            requestCount = 0;
        }
        
        requestCount++;
        
        if (requestCount > 100) // 100 requests per minute
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        _cache.Set(key, requestCount, TimeSpan.FromMinutes(1));
        
        await _next(context);
    }
}
```

## Custom Controllers

### Package Statistics Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly FeedContext _context;
    
    [HttpGet("package/{id}")]
    public async Task<IActionResult> GetPackageStats(string id)
    {
        var downloads = await _context.PackageDownloads
            .Where(d => d.PackageId == id)
            .GroupBy(d => d.Version)
            .Select(g => new
            {
                Version = g.Key,
                Downloads = g.Count(),
                UniqueUsers = g.Select(d => d.UserId).Distinct().Count(),
                LastDownload = g.Max(d => d.DownloadedAt)
            })
            .ToListAsync();
        
        return Ok(downloads);
    }
    
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingPackages(int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        
        var trending = await _context.PackageDownloads
            .Where(d => d.DownloadedAt >= since)
            .GroupBy(d => d.PackageId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new
            {
                PackageId = g.Key,
                Downloads = g.Count()
            })
            .ToListAsync();
        
        return Ok(trending);
    }
}
```

## Background Services

### Package Cleanup Service

```csharp
public class PackageCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PackageCleanupService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FeedContext>();
            
            // Delete packages older than 2 years with no downloads in last year
            var cutoff = DateTime.UtcNow.AddYears(-1);
            var oldPackages = await context.Packages
                .Where(p => p.CreatedAt < cutoff)
                .Where(p => !p.Downloads.Any(d => d.DownloadedAt > cutoff))
                .ToListAsync(stoppingToken);
            
            foreach (var package in oldPackages)
            {
                _logger.LogInformation($"Deleting old package: {package.Id} {package.Version}");
                context.Packages.Remove(package);
            }
            
            await context.SaveChangesAsync(stoppingToken);
            
            // Run once per day
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

**Register service:**

```csharp
builder.Services.AddHostedService<PackageCleanupService>();
```

## Custom Razor Tag Helpers

### Package Badge Tag Helper

```csharp
[HtmlTargetElement("package-badge")]
public class PackageBadgeTagHelper : TagHelper
{
    public string PackageId { get; set; }
    public string Version { get; set; }
    
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.Attributes.SetAttribute("class", "badge bg-primary");
        output.Content.SetHtmlContent($"{PackageId} v{Version}");
    }
}
```

**Usage in Razor:**

```html
<package-badge package-id="MyPackage" version="1.0.0" />
```

## Extending the Database

### Custom Migrations

Add custom migration logic:

```csharp
public partial class AddCustomFeatures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PackageReviews",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PackageId = table.Column<string>(nullable: false),
                Version = table.Column<string>(nullable: false),
                UserId = table.Column<int>(nullable: false),
                Rating = table.Column<int>(nullable: false),
                Comment = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PackageReviews", x => x.Id);
                table.ForeignKey(
                    name: "FK_PackageReviews_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }
}
```

## Best Practices

1. **Follow SOLID principles** - Keep implementations focused and maintainable
2. **Use dependency injection** - Register services properly in the DI container
3. **Handle errors gracefully** - Don't let custom code crash the application
4. **Log appropriately** - Use structured logging for debugging
5. **Test thoroughly** - Write unit and integration tests for custom code
6. **Document extensions** - Maintain documentation for custom features
7. **Version carefully** - Consider compatibility when updating

## Next Steps

- [View Customization Examples](customization.md)
- [Troubleshooting Guide](troubleshooting.md)
- [Template Reference](../reference/template-parameters.md)
