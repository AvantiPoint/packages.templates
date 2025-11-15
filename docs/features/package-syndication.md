# Package Syndication

Package syndication allows you to automatically mirror uploaded packages to other NuGet feeds, enabling scenarios like:

- **Multi-region deployment** - Replicate packages to feeds in different regions
- **Backup feeds** - Maintain a backup copy on a separate feed
- **Environment promotion** - Auto-promote packages from dev to staging/production
- **Partner distribution** - Share packages with external partners

## Overview

When syndication is enabled and configured, packages uploaded to your feed are automatically pushed to configured target feeds. This happens asynchronously after the initial upload completes.

### Features

- Multiple syndication targets
- Package group filtering (syndicate only specific packages)
- Symbol package support
- Automatic retry on failure
- Per-target configuration

## Configuration

Syndication is configured through the web UI and stored in the database.

### Enabling Syndication

In `appsettings.json`:

```json
{
  "Syndication": {
    "Enabled": true
  }
}
```

When enabled, the **Publish Targets** option appears in the Account menu for users with Publisher role.

## Managing Syndication Targets

### Adding a Target Feed

1. Sign in as a user with Publisher role
2. Navigate to **Account** → **Publish Targets**
3. Click **Add New Target**
4. Enter target details:
   - **Name** - Descriptive name (e.g., "Production Feed")
   - **Feed URL** - Target feed's push endpoint
   - **API Key** - Authentication key for the target feed
   - **Enabled** - Whether to sync to this target

### Target Configuration

Each syndication target stores:

```csharp
public class PublishTarget
{
    public int Id { get; set; }
    public string Name { get; set; }              // Display name
    public string FeedUrl { get; set; }           // Target feed URL
    public string ApiKey { get; set; }            // Encrypted API key
    public bool Enabled { get; set; }             // Enable/disable
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }     // Last successful sync
}
```

### Editing and Deleting Targets

- **Edit** - Update name, URL, API key, or enabled status
- **Disable** - Stop syncing without deleting the configuration
- **Delete** - Permanently remove the syndication target

## Package Groups

Package groups allow selective syndication - only packages matching specific criteria are synced to each target.

### Creating Package Groups

1. Navigate to **Account** → **Package Groups**
2. Click **Create New Group**
3. Enter group details:
   - **Name** - Group name (e.g., "Production Packages")
   - **Pattern** - Package ID pattern (supports wildcards)

### Pattern Matching

Package groups support wildcard patterns:

- `MyCompany.*` - Matches all packages starting with `MyCompany.`
- `*.Core` - Matches all packages ending with `.Core`
- `MyCompany.*.Services` - Matches packages like `MyCompany.Web.Services`
- `ExactPackageName` - Matches only that specific package

### Assigning Groups to Targets

Link package groups to syndication targets:

1. Edit a syndication target
2. Select which package groups to sync
3. Only packages matching group patterns will be syndicatged to that target

**Example scenario:**

- Group "Internal" with pattern `MyCompany.Internal.*`
- Group "Public" with pattern `MyCompany.Public.*`
- Target "Partner Feed" configured with group "Public"
- Result: Only `MyCompany.Public.*` packages sync to partner feed

## How Syndication Works

### Syndication Flow

1. User uploads package to your feed
2. Package is stored and indexed
3. `NuGetFeedActionHandler.OnPackageUploaded` is called
4. Syndication service identifies matching targets
5. For each enabled target:
   - Check if package matches target's package groups
   - Push package to target feed
   - Update last sync timestamp
   - Log success or failure

### Implementation

The syndication service is in `Services/SyndicationService.cs`:

```csharp
public class SyndicationService : ISyndicationService
{
    public async Task SyndicatePackage(string packageId, NuGetVersion version)
    {
        var targets = await GetEnabledTargetsForPackage(packageId);
        
        foreach (var target in targets)
        {
            try
            {
                await PushPackageToTarget(packageId, version, target);
                target.LastSyncAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    $"Failed to syndicate {packageId} {version} to {target.Name}");
            }
        }
    }
}
```

### Package Upload Handler

Syndication is triggered from `NuGetFeedActionHandler.cs`:

```csharp
public async Task OnPackageUploaded(string packageId, string version)
{
    _logger.LogInformation($"{User.Identity.Name} uploaded {packageId}.{version}.nupkg");
    
    // Send email notification
    await SendEmail(EmailTemplates.PackageUploaded, 
        $"Package Uploaded - {packageId} {version}", 
        packageId, version);

    // Syndicate to configured targets
    await _syndicationService.SyndicatePackage(packageId, NuGetVersion.Parse(version));
}
```

### Symbol Package Syndication

Symbol packages (.snupkg) are also syndicated:

```csharp
public async Task OnSymbolsUploaded(string packageId, string version)
{
    _logger.LogInformation($"{User.Identity.Name} uploaded {packageId}.{version}.snupkg");
    
    await SendEmail(EmailTemplates.SymbolsUploaded, 
        $"Symbols Uploaded - {packageId} {version}", 
        packageId, version);

    await _syndicationService.SyndicateSymbols(packageId, NuGetVersion.Parse(version));
}
```

## Error Handling

Syndication errors are logged but don't fail the original upload:

- Original package upload succeeds even if syndication fails
- Errors are logged with details
- Failed syndications can be retried manually
- Administrators are notified of persistent failures (if configured)

### Retry Logic

To add automatic retry:

```csharp
public async Task SyndicatePackage(string packageId, NuGetVersion version)
{
    var targets = await GetEnabledTargetsForPackage(packageId);
    
    foreach (var target in targets)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await PushPackageToTarget(packageId, version, target);
                target.LastSyncAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                break; // Success
            }
            catch (Exception ex) when (attempt < 3)
            {
                _logger.LogWarning(ex, 
                    $"Syndication attempt {attempt} failed, retrying...");
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    $"Syndication failed after {attempt} attempts");
            }
        }
    }
}
```

## Use Cases

### Multi-Region Deployment

Replicate packages to feeds in different geographic regions:

**Setup:**
- Target 1: "US Feed" - Feed in US data center
- Target 2: "EU Feed" - Feed in EU data center
- Target 3: "APAC Feed" - Feed in Asia-Pacific

**Benefit:** Developers worldwide pull packages from their nearest feed.

### Environment Promotion

Automatically promote packages through environments:

**Setup:**
- Main feed: Development feed (where packages are uploaded)
- Target 1: "Staging Feed" - All packages syndicated
- Target 2: "Production Feed" - Only packages matching "*.Production" group

**Workflow:**
1. Upload `MyPackage.1.0.0-dev` to dev feed → Syncs to staging
2. Upload `MyPackage.1.0.0` to dev feed → Syncs to both staging and production

### Partner Distribution

Share specific packages with external partners:

**Setup:**
- Group "Public APIs" with pattern `MyCompany.PublicApi.*`
- Target "Partner Feed" linked to "Public APIs" group

**Result:** Only public API packages are shared with partners.

### Backup Strategy

Maintain backup copies of all packages:

**Setup:**
- Target "Backup Feed" on separate infrastructure
- All package groups enabled
- Different storage backend (e.g., different cloud provider)

**Benefit:** Disaster recovery and business continuity.

## Advanced Configuration

### Conditional Syndication

Customize syndication logic in `SyndicationService.cs`:

```csharp
public async Task SyndicatePackage(string packageId, NuGetVersion version)
{
    // Only syndicate release versions (not pre-release)
    if (version.IsPrerelease)
    {
        _logger.LogInformation($"Skipping syndication of pre-release {packageId} {version}");
        return;
    }
    
    await SyndicateToTargets(packageId, version);
}
```

### Custom Syndication Triggers

Add manual syndication triggers:

```csharp
[HttpPost("api/admin/syndicate/{packageId}/{version}")]
[Authorize(Roles = FeedRoles.Publisher)]
public async Task<IActionResult> TriggerSyndication(string packageId, string version)
{
    await _syndicationService.SyndicatePackage(packageId, NuGetVersion.Parse(version));
    return Ok();
}
```

### Syndication Webhooks

Notify external systems of syndication events:

```csharp
public async Task OnSyndicationComplete(string packageId, NuGetVersion version, PublishTarget target)
{
    var webhook = new
    {
        PackageId = packageId,
        Version = version.ToString(),
        Target = target.Name,
        Timestamp = DateTime.UtcNow
    };
    
    await _httpClient.PostAsJsonAsync(target.WebhookUrl, webhook);
}
```

## Monitoring

### Logging

All syndication activity is logged:

```csharp
_logger.LogInformation($"Syndicating {packageId} {version} to {targets.Count} targets");
_logger.LogInformation($"Successfully syndicated {packageId} {version} to {target.Name}");
_logger.LogError(ex, $"Failed to syndicate {packageId} {version} to {target.Name}");
```

### Metrics

Track syndication metrics:

- Number of syndicated packages
- Success/failure rates per target
- Average syndication time
- Last successful sync time

Add metrics collection:

```csharp
public async Task SyndicatePackage(string packageId, NuGetVersion version)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        await SyndicateToTargets(packageId, version);
        _metrics.RecordSyndicationSuccess(stopwatch.Elapsed);
    }
    catch (Exception ex)
    {
        _metrics.RecordSyndicationFailure();
        throw;
    }
}
```

## Security Considerations

### API Key Storage

Target feed API keys are stored encrypted in the database:

- Use ASP.NET Core Data Protection for encryption
- Keys are never logged or exposed in UI
- Regular key rotation recommended

### Network Security

- Use HTTPS for all target feed URLs
- Validate SSL certificates
- Consider VPN or private network for sensitive targets
- Implement IP whitelisting on target feeds

### Access Control

- Only Publisher role can configure syndication
- Audit logging for all configuration changes
- Review targets regularly

## Troubleshooting

### Syndication Not Working

**Check:**
- Syndication is enabled in configuration
- Target is enabled
- Package matches target's package groups
- Target feed URL is correct
- Target API key is valid
- Network connectivity to target feed

### Partial Syndication

If some targets work but others don't:

1. Check logs for specific error messages
2. Verify each target's configuration individually
3. Test target API keys manually with NuGet CLI
4. Check network access to each target

### Performance Issues

If syndication is slow:

1. Reduce number of targets
2. Use faster network connections
3. Implement parallel syndication
4. Consider async/background processing

## Best Practices

1. **Test targets** before enabling in production
2. **Use package groups** to avoid syndicating unwanted packages
3. **Monitor syndication logs** for failures
4. **Set up alerts** for persistent failures
5. **Document target purposes** for team awareness
6. **Regular key rotation** for security
7. **Backup configurations** before making changes

## Next Steps

- [Learn about User Management](user-management.md)
- [Explore Customization Options](../advanced/customization.md)
- [Set up Monitoring](../advanced/troubleshooting.md)
