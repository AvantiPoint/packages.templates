# User Management

The template includes a complete user management system that allows organizations to control who can access and publish packages to the feed.

## User Roles

The system defines two primary roles:

### Consumer

Default role for all authenticated users.

**Permissions:**
- Browse and search packages
- Download packages
- Create and manage own API keys
- View own profile

### Publisher

Elevated role for users who can upload packages.

**Permissions:**
- All Consumer permissions
- Upload packages
- Upload symbol packages
- Unlist/delete packages
- Manage package groups
- Configure syndication targets
- View all users

## User Registration

### Automatic Registration

Users are automatically registered when they first sign in:

1. User authenticates via Azure AD
2. Application checks if user exists in database
3. If new user, creates account with:
   - Email from Azure AD
   - Display name from Azure AD
   - Consumer role (unless first user)
4. User is redirected to homepage

### First User Privilege

The first user to sign in is automatically granted the Publisher role:

```csharp
user = new User
{
    Email = email,
    Name = ctx.Principal.FindFirstValue("name"),
    PackagePublisher = !await feedContext.Users.AnyAsync()
};
```

This ensures there's always an initial administrator.

## Managing Users

### Viewing Users

Publishers can view all registered users:

1. Sign in as a user with Publisher role
2. Navigate to **Account** → **Users**
3. View list of all users with their roles

The user list displays:
- User name
- Email address
- Publisher status (Yes/No)
- Registration date
- Last activity

### Promoting Users to Publisher

To grant Publisher role to a user:

1. Navigate to **Account** → **Users**
2. Find the user in the list
3. Click **Edit** or **Promote**
4. Check **Package Publisher**
5. Save changes

The user will immediately gain Publisher permissions.

### Revoking Publisher Role

To remove Publisher role:

1. Navigate to **Account** → **Users**
2. Find the user in the list
3. Click **Edit**
4. Uncheck **Package Publisher**
5. Save changes

!!! warning
    Ensure at least one user maintains Publisher role to avoid being locked out of administrative functions.

## User Profile

Each user can manage their own profile:

### Viewing Profile

1. Sign in
2. Click on your name in the navigation
3. Select **Profile**

Profile displays:
- Display name
- Email address
- Role(s)
- Registration date
- API key count
- Recent activity

### Profile Icon

Users can customize their profile icon:

1. Navigate to **Profile** → **Icon**
2. Upload an image or choose initials-based icon
3. Icon appears throughout the application

## API Key Management

Users manage their own API keys through the web interface.

### Creating API Keys

1. Navigate to **Account** → **API Keys**
2. Click **Create New Token**
3. Enter a description (e.g., "Build Server", "Development")
4. Click **Create**
5. Copy the generated key (shown only once!)

### Viewing API Keys

The API Keys page shows:
- Token description/name
- Creation date
- Last used date
- Revocation status

API key values are never displayed after creation.

### Revoking API Keys

To revoke an API key:

1. Navigate to **Account** → **API Keys**
2. Find the key to revoke
3. Click **Revoke**
4. Confirm the action

Revoked keys cannot be used for any operations.

### Best Practices for API Keys

- **Use descriptive names** - Clearly identify each key's purpose
- **One key per system** - Use different keys for build servers, dev machines, etc.
- **Regular rotation** - Create new keys and revoke old ones periodically
- **Immediate revocation** - Revoke compromised keys immediately
- **Track usage** - Review last used dates to identify stale keys

## User Data Model

The `User` entity stores user information:

```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }           // From Azure AD
    public string Name { get; set; }            // Display name
    public bool PackagePublisher { get; set; }  // Publisher role
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<AuthToken> AuthTokens { get; set; }
    public virtual ICollection<PackageDownload> Downloads { get; set; }
}
```

## Authentication Tokens

API keys are stored as `AuthToken` entities:

```csharp
public class AuthToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Key { get; set; }             // Hashed API key
    public string Description { get; set; }      // User-provided name
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }    // Optional expiration
    public bool Revoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    // Navigation property
    public virtual User User { get; set; }
}
```

### Token Validation

Tokens are validated before use:

```csharp
public bool IsValid()
{
    return !Revoked && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}
```

## Activity Tracking

The system tracks user activity for auditing and security.

### Package Downloads

Each package download is recorded:

```csharp
public class PackageDownload
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PackageId { get; set; }
    public string Version { get; set; }
    public IPAddress RemoteIp { get; set; }
    public DateTime DownloadedAt { get; set; }
    
    public virtual User User { get; set; }
}
```

This enables:
- Download statistics per package
- User activity monitoring
- New IP detection for security alerts
- Usage analytics

### Login Tracking

User logins are tracked:

```csharp
user.LastLoginAt = DateTime.UtcNow;
await context.SaveChangesAsync();
```

## Customizing User Management

### Adding Custom User Fields

Add properties to the `User` model:

```csharp
public class User
{
    // ... existing properties ...
    
    public string Department { get; set; }
    public string EmployeeId { get; set; }
    public string PhoneNumber { get; set; }
}
```

Create and apply a migration:

```bash
dotnet ef migrations add AddUserCustomFields
dotnet ef database update
```

### Custom Authorization Logic

Implement custom authorization rules:

```csharp
public Task<bool> CanDownloadPackage(string packageId, string version)
{
    // Department-based access control
    if (packageId.StartsWith("Internal."))
    {
        var department = User.FindFirstValue("Department");
        return Task.FromResult(department == "Engineering");
    }
    
    return Task.FromResult(User.IsInRole(FeedRoles.Consumer));
}
```

### External User Directory

Integrate with external user directories:

```csharp
public async Task SyncUsersFromDirectory()
{
    var directoryUsers = await _directoryService.GetAllUsersAsync();
    
    foreach (var directoryUser in directoryUsers)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == directoryUser.Email);
        
        if (user == null)
        {
            user = new User
            {
                Email = directoryUser.Email,
                Name = directoryUser.Name,
                Department = directoryUser.Department,
                PackagePublisher = directoryUser.IsInGroup("NuGet Publishers")
            };
            _context.Users.Add(user);
        }
        else
        {
            user.Name = directoryUser.Name;
            user.PackagePublisher = directoryUser.IsInGroup("NuGet Publishers");
        }
    }
    
    await _context.SaveChangesAsync();
}
```

## Security Considerations

### API Key Security

- Keys are hashed before storage using secure hashing
- Original key value is never stored
- Keys cannot be recovered if lost
- Users must create new keys if old ones are lost

### Access Control

- All user management pages require authentication
- Publisher-only pages check role authorization
- API key operations are restricted to key owner
- Admin functions require Publisher role

### Audit Logging

Enable comprehensive audit logging:

```csharp
_logger.LogInformation($"User {currentUser.Email} promoted {targetUser.Email} to Publisher");
_logger.LogWarning($"User {user.Email} revoked API key: {token.Description}");
_logger.LogInformation($"New user registered: {user.Email}");
```

## Best Practices

1. **Least Privilege** - Grant Publisher role only when needed
2. **Regular Review** - Periodically review user list and permissions
3. **Audit Activity** - Monitor logs for suspicious activity
4. **Key Rotation** - Encourage users to rotate API keys regularly
5. **Revoke Promptly** - Revoke access for departed users immediately
6. **Document Roles** - Maintain documentation of who has Publisher access
7. **Monitor Downloads** - Review download activity for unusual patterns

## Common Scenarios

### Onboarding a New User

1. User signs in via Azure AD (auto-registered)
2. User creates their first API key
3. Publisher reviews and grants Publisher role if needed
4. User can now push packages (if Publisher)

### Offboarding a User

1. Publisher revokes user's Publisher role (if applicable)
2. Publisher or user revokes all API keys
3. User account remains for audit history
4. Optionally mark user as inactive

### Key Compromise

If an API key is compromised:

1. User immediately revokes the compromised key
2. User creates a new key
3. User updates systems using the old key
4. Publisher reviews download logs for suspicious activity
5. Security team is notified if needed

### Bulk User Management

For organizations with many users:

1. Implement user sync from Azure AD groups
2. Automatically assign roles based on group membership
3. Schedule regular sync operations
4. Maintain audit trail of permission changes

## Troubleshooting

### User Cannot Sign In

**Possible causes:**
- Not in Azure AD tenant
- Azure AD app configuration incorrect
- Browser cookies disabled

**Solutions:**
- Verify user is in the configured Azure AD tenant
- Check Azure AD app settings
- Clear browser cache and cookies

### API Key Not Working

**Check:**
- Key hasn't been revoked
- Key hasn't expired
- User has appropriate role (Publisher for push)
- Key is correctly configured in NuGet client

### Cannot Grant Publisher Role

**Verify:**
- Current user has Publisher role
- Target user exists in database
- Database connection is working

## Next Steps

- [Configure Email Notifications](email-notifications.md)
- [Set up Package Syndication](package-syndication.md)
- [Learn about Customization](../advanced/customization.md)
