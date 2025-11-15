# Storage Options

Configure package storage for your NuGet feed using either the local file system or Azure Blob Storage.

## Overview

The template supports two storage backends:

1. **File System Storage** - Store packages on local disk or network shares
2. **Azure Blob Storage** - Store packages in Azure cloud storage

## File System Storage

Store packages on the local file system or network-attached storage.

### Configuration

```json
{
  "Storage": {
    "Type": "FileSystem",
    "Path": "Packages"
  }
}
```

### When to Use

**Development:**
- Local testing and development
- Quick setup without cloud dependencies
- No additional costs

**Production (Small Scale):**
- Single server deployments
- Predictable, low package volumes
- Direct disk access required

### Directory Structure

Packages are stored in a hierarchical folder structure:

```
Packages/
├── mypackage/
│   ├── 1.0.0/
│   │   ├── mypackage.1.0.0.nupkg
│   │   └── mypackage.1.0.0.nupkg.sha512
│   └── 2.0.0/
│       ├── mypackage.2.0.0.nupkg
│       └── mypackage.2.0.0.nupkg.sha512
└── anotherpackage/
    └── 1.0.0/
        ├── anotherpackage.1.0.0.nupkg
        └── anotherpackage.1.0.0.nupkg.sha512
```

### Network Storage

You can use UNC paths or mapped network drives:

```json
{
  "Storage": {
    "Type": "FileSystem",
    "Path": "\\\\server\\share\\Packages"
  }
}
```

or

```json
{
  "Storage": {
    "Type": "FileSystem",
    "Path": "Z:\\Packages"
  }
}
```

### Backup Strategies

**File-based backups:**
- Use robocopy, rsync, or backup software
- Copy entire Packages directory
- Maintain version history if desired

**Example backup script (Windows):**
```powershell
$source = "C:\Packages"
$destination = "\\backup-server\nuget-backup\Packages"
robocopy $source $destination /MIR /R:3 /W:5
```

**Example backup script (Linux):**
```bash
rsync -avz --delete /var/packages/ backup-server:/backup/nuget-packages/
```

## Azure Blob Storage

Store packages in Azure Blob Storage for scalability and redundancy.

### Configuration

```json
{
  "Storage": {
    "Type": "AzureBlobStorage",
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net",
    "Container": "packages"
  }
}
```

### When to Use

**Production (Recommended):**
- Multi-server deployments
- High availability requirements
- Large package volumes
- Geo-redundant storage needed
- Cost-effective scaling

**Benefits:**
- Automatic replication and redundancy
- No storage capacity limits
- Pay only for what you use
- Integration with Azure CDN
- Built-in backup and versioning

### Setting Up Azure Blob Storage

#### Create Storage Account

```bash
az storage account create \
  --name mynugetfeedstorage \
  --resource-group nuget-feed-rg \
  --location eastus \
  --sku Standard_LRS \
  --kind StorageV2
```

#### Create Container

```bash
az storage container create \
  --name packages \
  --account-name mynugetfeedstorage \
  --public-access off
```

#### Get Connection String

```bash
az storage account show-connection-string \
  --name mynugetfeedstorage \
  --resource-group nuget-feed-rg \
  --output tsv
```

### Connection String Formats

**Account Key:**
```
DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=abc123...;EndpointSuffix=core.windows.net
```

**Managed Identity (Recommended):**
```
DefaultEndpointsProtocol=https;AccountName=myaccount;EndpointSuffix=core.windows.net
```

When using Managed Identity, grant the App Service Managed Identity the "Storage Blob Data Contributor" role on the storage account.

### Redundancy Options

Azure offers several redundancy options:

| SKU | Description | Use Case |
|-----|-------------|----------|
| `Standard_LRS` | Locally redundant (3 copies in one datacenter) | Development, low-cost |
| `Standard_ZRS` | Zone redundant (3 copies across zones) | Production, single region |
| `Standard_GRS` | Geo-redundant (6 copies across regions) | High availability |
| `Standard_RAGRS` | Read-access geo-redundant | HA + read access |
| `Premium_LRS` | Premium SSD storage | High performance |

### Blob Versioning

Enable versioning for package history:

```bash
az storage account blob-service-properties update \
  --account-name mynugetfeedstorage \
  --enable-versioning true
```

### Soft Delete

Protect against accidental deletion:

```bash
az storage account blob-service-properties update \
  --account-name mynugetfeedstorage \
  --enable-delete-retention true \
  --delete-retention-days 30
```

### Lifecycle Management

Automatically move old packages to cool/archive storage:

```json
{
  "rules": [
    {
      "enabled": true,
      "name": "MoveOldPackagesToCool",
      "type": "Lifecycle",
      "definition": {
        "filters": {
          "blobTypes": ["blockBlob"]
        },
        "actions": {
          "baseBlob": {
            "tierToCool": {
              "daysAfterModificationGreaterThan": 90
            },
            "tierToArchive": {
              "daysAfterModificationGreaterThan": 365
            }
          }
        }
      }
    }
  ]
}
```

### CDN Integration

Use Azure CDN for faster package downloads worldwide:

1. Create CDN endpoint pointing to blob storage
2. Configure caching rules
3. Update NuGet client URLs to use CDN

## Performance Comparison

### File System

**Pros:**
- Simple setup and configuration
- Direct disk I/O (fast for local access)
- No external dependencies
- No additional costs

**Cons:**
- Limited to single server (or complex NAS setup)
- Manual backup and replication
- Storage capacity limited to disk size
- No built-in redundancy

### Azure Blob Storage

**Pros:**
- Highly scalable (no capacity limits)
- Built-in redundancy and geo-replication
- CDN integration for global distribution
- Automatic backup and versioning
- Multi-server support out of the box

**Cons:**
- Requires Azure subscription
- Network latency for each operation
- Usage costs (though often minimal)
- More complex initial setup

## Migration Between Storage Backends

### File System to Azure Blob

1. Create Azure Storage account and container
2. Upload all packages to blob storage:

```bash
az storage blob upload-batch \
  --destination packages \
  --source ./Packages \
  --account-name mynugetfeedstorage \
  --pattern "**/*.nupkg"
```

3. Update configuration to use Azure Blob Storage
4. Restart application
5. Verify packages are accessible
6. Remove local packages after validation

### Azure Blob to File System

1. Download all packages from blob storage:

```bash
az storage blob download-batch \
  --source packages \
  --destination ./Packages \
  --account-name mynugetfeedstorage \
  --pattern "**/*.nupkg"
```

2. Update configuration to use File System
3. Restart application
4. Verify packages are accessible

## Cost Optimization

### Azure Blob Storage Costs

**Factors affecting cost:**
- Storage capacity used
- Number of operations (read/write)
- Data transfer (egress)
- Redundancy tier chosen

**Tips to reduce costs:**
1. Use lifecycle policies to move old packages to cool/archive tiers
2. Enable compression for package uploads
3. Use LRS instead of GRS for non-critical environments
4. Monitor and clean up unused packages
5. Use Azure CDN to reduce blob storage egress charges

## Monitoring and Diagnostics

### File System Monitoring

Monitor disk space:

```powershell
# Windows
Get-PSDrive C | Select-Object Used,Free

# Linux
df -h /var/packages
```

Set up alerts for low disk space.

### Azure Blob Storage Monitoring

Enable metrics and logging:

```bash
az monitor diagnostic-settings create \
  --name nuget-storage-diagnostics \
  --resource /subscriptions/{sub-id}/resourceGroups/nuget-feed-rg/providers/Microsoft.Storage/storageAccounts/mynugetfeedstorage \
  --logs '[{"category": "StorageRead","enabled": true},{"category": "StorageWrite","enabled": true}]' \
  --metrics '[{"category": "Transaction","enabled": true}]' \
  --workspace /subscriptions/{sub-id}/resourcegroups/nuget-feed-rg/providers/microsoft.operationalinsights/workspaces/nuget-logs
```

View metrics in Azure Portal under **Monitoring** → **Metrics**.

## Security Considerations

### File System

- Use appropriate NTFS/file permissions
- Restrict access to the Packages directory
- Regular security updates on host OS
- Network isolation if using network shares

### Azure Blob Storage

- Use private containers (no public access)
- Enable encryption at rest (on by default)
- Use Managed Identity instead of account keys when possible
- Enable firewall rules to restrict access
- Regular access key rotation
- Enable blob versioning for audit trail

## Best Practices

1. **Choose based on scale** - File system for small, single-server; Azure Blob for production
2. **Regular backups** - Regardless of storage type, maintain backups
3. **Monitor storage** - Set up alerts for capacity and performance
4. **Test recovery** - Regularly test backup restoration procedures
5. **Document configuration** - Keep notes on storage setup for team reference
6. **Plan for growth** - Consider future needs when choosing storage type

## Troubleshooting

### Cannot Access File System Storage

- Verify path exists and is accessible
- Check file permissions
- Ensure no antivirus blocking access
- Verify network connectivity (for UNC paths)

### Cannot Access Azure Blob Storage

- Verify connection string is correct
- Check network connectivity to Azure
- Verify container exists
- Check storage account firewall rules
- Ensure sufficient permissions (Managed Identity or account key)

## Next Steps

- [Deploy to Azure](azure.md)
- [Configure Azure AD](azure-ad.md)
- [Learn about Customization](../advanced/customization.md)
