# Troubleshooting

Common issues and solutions when working with the AvantiPoint Packages Template.

## Installation Issues

### Template Not Found After Installation

**Problem:** `dotnet new packagefeed` reports template not found.

**Solutions:**
1. Reinitialize template cache:
   ```bash
   dotnet new --debug:reinit
   ```

2. Verify template is installed:
   ```bash
   dotnet new list | grep packagefeed
   ```

3. Reinstall the template:
   ```bash
   dotnet new uninstall AvantiPoint.Packages.Templates
   dotnet new install AvantiPoint.Packages.Templates
   ```

### Template Installation Fails

**Problem:** Installation fails with package errors.

**Solutions:**
1. Update .NET SDK to latest version
2. Clear NuGet caches:
   ```bash
   dotnet nuget locals all --clear
   ```
3. Check NuGet sources are accessible:
   ```bash
   dotnet nuget list source
   ```

## Database Issues

### Cannot Connect to Database

**Problem:** Application fails to connect to SQL Server.

**Symptoms:**
- Error: "A network-related or instance-specific error occurred"
- Error: "Login failed for user"

**Solutions:**

1. **Verify SQL Server is running:**
   ```bash
   # Windows - LocalDB
   sqllocaldb info
   sqllocaldb start mssqllocaldb
   
   # Windows - SQL Server service
   Get-Service -Name MSSQLSERVER
   ```

2. **Check connection string:**
   - Verify server name is correct
   - Check database name exists
   - Ensure authentication method (Windows/SQL) matches credentials

3. **Test connection:**
   ```bash
   sqlcmd -S (localdb)\mssqllocaldb -Q "SELECT @@VERSION"
   ```

4. **Firewall rules:**
   - Ensure SQL Server port (default 1433) is not blocked
   - Check Windows Firewall settings

### Migration Errors

**Problem:** Database migrations fail to apply.

**Solutions:**

1. **Reset database (development only):**
   ```bash
   dotnet ef database drop
   dotnet ef database update
   ```

2. **Check migration history:**
   ```bash
   dotnet ef migrations list
   ```

3. **Create new migration:**
   ```bash
   dotnet ef migrations add FixIssue
   dotnet ef database update
   ```

4. **Manual SQL execution:**
   ```bash
   dotnet ef migrations script > migration.sql
   # Review and execute SQL manually
   ```

## Authentication Issues

### Azure AD Sign-In Fails

**Problem:** Users cannot sign in through Azure AD.

**Symptoms:**
- Error: "AADSTS50011: Reply URL mismatch"
- Error: "AADSTS700016: Application not found"
- Redirect loop after sign-in

**Solutions:**

1. **Verify Redirect URI:**
   - Must match exactly (including protocol and port)
   - No trailing slash
   - Check both Azure AD config and application settings

2. **Check Tenant/Client IDs:**
   ```json
   {
     "AzureAd": {
       "TenantId": "correct-tenant-id",
       "ClientId": "correct-client-id"
     }
   }
   ```

3. **Enable ID tokens:**
   - In Azure Portal → App Registration → Authentication
   - Check "ID tokens" under Implicit grant

4. **Clear browser cache:**
   - Sign out completely
   - Clear cookies and cache
   - Try incognito/private mode

5. **Check application logs:**
   ```bash
   dotnet run --environment Development
   # Look for authentication-related errors
   ```

### API Key Authentication Fails

**Problem:** NuGet operations fail with 401 Unauthorized.

**Symptoms:**
- `dotnet nuget push` returns 401
- Package download returns 401

**Solutions:**

1. **Verify API key is correct:**
   - Check key wasn't truncated when copying
   - Ensure no extra spaces

2. **Check key hasn't been revoked:**
   - Log in to web UI
   - Navigate to Account → API Keys
   - Verify key status

3. **Confirm user has Publisher role:**
   - Required for package push
   - Check in web UI under Account → Users

4. **Test with verbose logging:**
   ```bash
   dotnet nuget push package.nupkg \
     --source https://localhost:7000/v3/index.json \
     --api-key your-key \
     --verbosity detailed
   ```

## Package Upload Issues

### Package Push Fails

**Problem:** Cannot upload packages to feed.

**Common Errors:**

**"Package already exists":**
```
Error: Response status code does not indicate success: 409 (Conflict).
```
**Solution:** Package version already exists. Increment version number or enable package overwrite (not recommended).

**"Payload too large":**
```
Error: 413 (Payload Too Large)
```
**Solution:** Package exceeds size limits. Check configuration:
```csharp
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue;
});
```

**"Unauthorized":**
```
Error: Response status code does not indicate success: 401 (Unauthorized).
```
**Solution:** See "API Key Authentication Fails" above.

### Package Download Fails

**Problem:** Cannot restore packages from feed.

**Solutions:**

1. **Check NuGet source configuration:**
   ```bash
   dotnet nuget list source
   ```

2. **Test source availability:**
   ```bash
   dotnet nuget search MyPackage --source https://localhost:7000/v3/index.json
   ```

3. **Check credentials:**
   ```bash
   dotnet nuget add source https://localhost:7000/v3/index.json \
     --name MyFeed \
     --username your-email \
     --password your-api-key \
     --store-password-in-clear-text
   ```

4. **Clear NuGet cache:**
   ```bash
   dotnet nuget locals all --clear
   ```

## Email Issues

### Emails Not Sending

**Problem:** Email notifications are not being sent.

**Solutions:**

1. **Verify email is enabled:**
   ```json
   {
     "Email": {
       "Enabled": true
     }
   }
   ```

2. **Check provider configuration:**
   - Verify API key is correct
   - Test API key with provider's test endpoint

3. **Check application logs:**
   ```bash
   # Look for email-related errors
   grep -i "email" /path/to/logs
   ```

4. **Test email service directly:**
   ```csharp
   var emailService = app.Services.GetRequiredService<IEmailService>();
   await emailService.SendEmail("test-template", 
     new MailAddress("test@example.com"), 
     "Test Subject", 
     new { });
   ```

### Emails Going to Spam

**Problem:** Email notifications end up in spam folder.

**Solutions:**

1. **Set up SPF record:**
   ```
   v=spf1 include:sendgrid.net ~all
   ```

2. **Set up DKIM:**
   - Configure in SendGrid/Postmark dashboard
   - Add DNS records as instructed

3. **Verify sender domain:**
   - Use verified domain for FromAddress
   - Don't use generic domains like gmail.com

4. **Improve email content:**
   - Avoid spam trigger words
   - Include plain text version
   - Add unsubscribe link

## Storage Issues

### Azure Blob Storage Connection Fails

**Problem:** Cannot connect to Azure Blob Storage.

**Symptoms:**
- Error: "Server failed to authenticate the request"
- Error: "The remote name could not be resolved"

**Solutions:**

1. **Verify connection string:**
   ```json
   {
     "Storage": {
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=name;AccountKey=key;EndpointSuffix=core.windows.net"
     }
   }
   ```

2. **Check storage account exists:**
   ```bash
   az storage account show --name mystorageaccount
   ```

3. **Verify container exists:**
   ```bash
   az storage container list --account-name mystorageaccount
   ```

4. **Check firewall rules:**
   - In Azure Portal → Storage Account → Networking
   - Ensure application IP is allowed

5. **Test with Storage Explorer:**
   - Use Azure Storage Explorer to verify connectivity

### File System Storage Issues

**Problem:** Cannot read/write packages to file system.

**Solutions:**

1. **Check directory exists:**
   ```bash
   ls -la /path/to/Packages
   ```

2. **Verify permissions:**
   ```bash
   # Linux/Mac
   chmod 755 /path/to/Packages
   
   # Windows - Run as Administrator
   icacls C:\Packages /grant Users:(OI)(CI)F
   ```

3. **Check disk space:**
   ```bash
   # Linux/Mac
   df -h /path/to/Packages
   
   # Windows
   Get-PSDrive C
   ```

## Performance Issues

### Slow Package Search

**Problem:** Package search is slow.

**Solutions:**

1. **Add database indexes:**
   ```csharp
   migrationBuilder.CreateIndex(
       name: "IX_Packages_Id_Version",
       table: "Packages",
       columns: new[] { "Id", "Version" });
   ```

2. **Enable database query caching**

3. **Use Azure SQL with higher tier** (for production)

4. **Implement pagination** for large result sets

### High Memory Usage

**Problem:** Application consumes excessive memory.

**Solutions:**

1. **Check for memory leaks:**
   - Use dotMemory or PerfView for profiling
   - Look for undisposed resources

2. **Optimize package streaming:**
   - Ensure packages are streamed, not loaded into memory
   - Check buffer sizes

3. **Configure garbage collection:**
   ```json
   {
     "System.GC.Server": true,
     "System.GC.Concurrent": true
   }
   ```

## Deployment Issues

### Azure Deployment Fails

**Problem:** Deployment to Azure App Service fails.

**Solutions:**

1. **Check deployment logs:**
   ```bash
   az webapp log deployment show \
     --name myapp \
     --resource-group mygroup
   ```

2. **Verify .NET runtime:**
   ```bash
   az webapp config show \
     --name myapp \
     --resource-group mygroup \
     --query linuxFxVersion
   ```

3. **Check application settings:**
   ```bash
   az webapp config appsettings list \
     --name myapp \
     --resource-group mygroup
   ```

4. **Review startup logs:**
   ```bash
   az webapp log tail \
     --name myapp \
     --resource-group mygroup
   ```

### Application Crashes on Startup

**Problem:** Application fails to start.

**Solutions:**

1. **Check startup errors:**
   - Enable detailed error messages
   - Review exception stack traces

2. **Verify all dependencies:**
   - Database is accessible
   - Configuration is complete
   - External services are reachable

3. **Test locally:**
   ```bash
   dotnet run --environment Production
   ```

4. **Check health endpoint:**
   ```bash
   curl https://myapp.azurewebsites.net/health
   ```

## Getting Help

If you still have issues:

1. **Check logs:**
   - Application logs
   - Azure diagnostics
   - IIS logs (if applicable)

2. **Enable verbose logging:**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

3. **Open GitHub issue:**
   - [AvantiPoint.Packages.Templates Issues](https://github.com/AvantiPoint/packages.templates/issues)
   - Include error messages, logs, and steps to reproduce

4. **Community support:**
   - Stack Overflow (tag: avantipoint-packages)
   - GitHub Discussions

## Next Steps

- [Review Configuration Options](../reference/configuration-options.md)
- [Learn about Customization](customization.md)
- [Check Azure Deployment Guide](../hosting/azure.md)
