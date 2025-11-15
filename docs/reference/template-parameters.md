# Template Parameters

This reference documents all parameters available when creating a new project from the AvantiPoint Packages Template.

## Usage

Parameters are specified when creating a new project using the `dotnet new` command:

```bash
dotnet new packagefeed -n MyFeed [parameters]
```

## Available Parameters

### ADDomain

**Type:** `string`  
**Default:** `contoso.com`  
**Description:** Your Azure Active Directory domain name.

**Example:**
```bash
dotnet new packagefeed -n MyFeed --ADDomain "mycompany.com"
```

This value is used in the Azure AD configuration in `appsettings.Development.json`:

```json
{
  "AzureAd": {
    "Domain": "mycompany.com"
  }
}
```

### ADTenantId

**Type:** `string`  
**Default:** `00000000-0000-0000-0000-000000000000`  
**Description:** Your Azure Active Directory Tenant ID (Directory ID).

**Example:**
```bash
dotnet new packagefeed -n MyFeed --ADTenantId "12345678-1234-1234-1234-123456789012"
```

**Finding your Tenant ID:**
1. Open the [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory**
3. Click **Overview**
4. Copy the **Tenant ID** value

### ADClientId

**Type:** `string`  
**Default:** `11111111-1111-1111-1111-111111111111`  
**Description:** Your Azure AD Application (client) ID.

**Example:**
```bash
dotnet new packagefeed -n MyFeed --ADClientId "87654321-4321-4321-4321-210987654321"
```

**Finding your Client ID:**
1. Open the [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Select your application
4. Copy the **Application (client) ID** value

### SendGridApiKey

**Type:** `string`  
**Default:** (empty)  
**Description:** SendGrid API key for sending email notifications.

**Example:**
```bash
dotnet new packagefeed -n MyFeed --SendGridApiKey "SG.abc123..."
```

Leave empty if you plan to:
- Configure it later
- Use Postmark instead
- Disable email notifications

### PostmarkApiKey

**Type:** `string`  
**Default:** (empty)  
**Description:** Postmark API key for sending email notifications.

**Example:**
```bash
dotnet new packagefeed -n MyFeed --PostmarkApiKey "abc123-def456..."
```

Leave empty if you plan to:
- Configure it later
- Use SendGrid instead
- Disable email notifications

## Complete Example

Create a fully configured project:

```bash
dotnet new packagefeed -n MyCompanyFeed \
  --ADDomain "mycompany.com" \
  --ADTenantId "12345678-1234-1234-1234-123456789012" \
  --ADClientId "87654321-4321-4321-4321-210987654321" \
  --SendGridApiKey "SG.your-api-key-here"
```

## Generated Files

The template generates files with parameter values substituted:

### appsettings.Development.json

```json
{
  "AzureAd": {
    "Domain": "[ADDomain]",
    "TenantId": "[ADTenantId]",
    "ClientId": "[ADClientId]"
  },
  "Email": {
    "SendGrid": {
      "ApiKey": "[SendGridApiKey]"
    },
    "Postmark": {
      "ApiKey": "[PostmarkApiKey]"
    }
  }
}
```

## Updating Parameters After Creation

If you need to change values after creating the project:

### Option 1: Edit Configuration Files

Directly edit `appsettings.Development.json` or `appsettings.Production.json`.

### Option 2: Use User Secrets (Development)

```bash
dotnet user-secrets set "AzureAd:TenantId" "new-tenant-id"
dotnet user-secrets set "AzureAd:ClientId" "new-client-id"
dotnet user-secrets set "Email:SendGrid:ApiKey" "new-api-key"
```

### Option 3: Environment Variables (Production)

```bash
export AzureAd__TenantId="new-tenant-id"
export AzureAd__ClientId="new-client-id"
export Email__SendGrid__ApiKey="new-api-key"
```

### Option 4: Azure App Service Configuration

In the Azure Portal:
1. Navigate to your App Service
2. Go to **Configuration** → **Application settings**
3. Add or update settings

## Placeholder Values

The template uses the following placeholders that are replaced during project creation:

| Placeholder | Replaced By | Parameter |
|-------------|-------------|-----------|
| `ReplaceDomain` | Domain value | `ADDomain` |
| `ReplaceTenantId` | Tenant ID | `ADTenantId` |
| `ReplaceClientId` | Client ID | `ADClientId` |
| `ReplaceSendGridApiKey` | SendGrid API key | `SendGridApiKey` |
| `ReplacePostmarkApiKey` | Postmark API key | `PostmarkApiKey` |

## Template Metadata

The template is identified by:

- **Short Name:** `packagefeed`
- **Name:** `AvantiPoint.Packages.Template`
- **Identity:** `AvantiPoint.Packages.Templates.CSharp`
- **Language:** `C#`
- **Type:** `project`
- **Tags:** `aspnetcore`, `nuget`

## See Also

- [Installation Guide](../getting-started/installation.md)
- [Configuration Reference](configuration-options.md)
- [Quick Start](../getting-started/quick-start.md)
