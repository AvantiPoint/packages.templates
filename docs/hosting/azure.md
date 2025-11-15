# Azure Deployment

This guide covers deploying your NuGet feed to Azure for production use.

## Deployment Options

### Azure App Service (Recommended)

Azure App Service provides a fully managed platform for hosting web applications.

**Benefits:**
- Easy deployment and scaling
- Built-in load balancing
- Automatic OS and runtime patching
- Integration with Azure AD
- SSL certificates included
- CI/CD integration

**Pricing:** Pay for the App Service Plan tier you choose.

### Azure Container Instances

Run your feed in containers for maximum flexibility.

**Benefits:**
- Container-based deployment
- Quick scaling
- Isolated environments
- Custom runtime configurations

### Azure Kubernetes Service (AKS)

For large-scale deployments requiring advanced orchestration.

**Benefits:**
- High availability and scale
- Advanced deployment strategies
- Multi-region support
- Complex networking scenarios

## Deploying to Azure App Service

### Prerequisites

- Azure subscription
- Azure CLI installed locally
- Application code ready to deploy
- Azure AD app configured

### Step 1: Create Required Resources

Create resource group:

```bash
az group create \
  --name nuget-feed-rg \
  --location eastus
```

Create App Service Plan:

```bash
az appservice plan create \
  --name nuget-feed-plan \
  --resource-group nuget-feed-rg \
  --sku P1V2 \
  --is-linux
```

Create Web App:

```bash
az webapp create \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --plan nuget-feed-plan \
  --runtime "DOTNETCORE:9.0"
```

### Step 2: Create Azure SQL Database

Create SQL Server:

```bash
az sql server create \
  --name mynugetfeed-sql \
  --resource-group nuget-feed-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password 'YourPassword123!'
```

Create database:

```bash
az sql db create \
  --resource-group nuget-feed-rg \
  --server mynugetfeed-sql \
  --name NuGetFeed \
  --service-objective S0
```

Configure firewall to allow Azure services:

```bash
az sql server firewall-rule create \
  --resource-group nuget-feed-rg \
  --server mynugetfeed-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Step 3: Create Azure Storage Account (Optional)

For Azure Blob Storage:

```bash
az storage account create \
  --name mynugetfeedstorage \
  --resource-group nuget-feed-rg \
  --location eastus \
  --sku Standard_LRS
```

Create container:

```bash
az storage container create \
  --name packages \
  --account-name mynugetfeedstorage
```

### Step 4: Configure Application Settings

Set connection string:

```bash
# Get connection string
CONN_STRING=$(az sql db show-connection-string \
  --server mynugetfeed-sql \
  --name NuGetFeed \
  --client ado.net \
  --output tsv)

# Set in App Service
az webapp config connection-string set \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="$CONN_STRING"
```

Set application settings:

```bash
az webapp config appsettings set \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --settings \
    AzureAd__TenantId="your-tenant-id" \
    AzureAd__ClientId="your-client-id" \
    AzureAd__Domain="yourcompany.com" \
    Email__SendGrid__ApiKey="your-sendgrid-key" \
    Storage__Type="AzureBlobStorage" \
    Storage__ConnectionString="your-storage-connection-string" \
    Storage__Container="packages"
```

### Step 5: Deploy Application

#### Option A: Deploy from Visual Studio

1. Right-click project → **Publish**
2. Select **Azure** → **Azure App Service (Windows/Linux)**
3. Select your App Service
4. Click **Publish**

#### Option B: Deploy via Azure CLI

```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip *
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --src deploy.zip
```

#### Option C: Deploy via GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Publish
      run: dotnet publish --configuration Release --output ./publish
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: my-nuget-feed
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### Step 6: Update Azure AD Redirect URI

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Select your application
4. Go to **Authentication**
5. Add redirect URI: `https://my-nuget-feed.azurewebsites.net/signin-oidc`
6. Save changes

### Step 7: Apply Database Migrations

Connect to your deployed app and run migrations:

```bash
# Get connection string from Azure
CONN_STRING=$(az webapp config connection-string list \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --query "[0].value" -o tsv)

# Run migrations
dotnet ef database update --connection "$CONN_STRING"
```

Or enable automatic migrations in `Program.cs` (already configured):

```csharp
await app.InitializeDatabaseContext();
```

### Step 8: Configure Custom Domain (Optional)

Add custom domain:

```bash
az webapp config hostname add \
  --webapp-name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --hostname nuget.yourcompany.com
```

Enable HTTPS:

```bash
az webapp config ssl bind \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --certificate-thumbprint YOUR_CERT_THUMBPRINT \
  --ssl-type SNI
```

## High Availability Configuration

### Enable Health Checks

Add in `Program.cs`:

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .AddAzureBlobStorage(builder.Configuration["Storage:ConnectionString"]);

app.MapHealthChecks("/health");
```

Configure in Azure:

```bash
az webapp config set \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --health-check-path "/health"
```

### Auto-Scaling

Configure scale rules:

```bash
az monitor autoscale create \
  --name nuget-feed-autoscale \
  --resource-group nuget-feed-rg \
  --resource my-nuget-feed \
  --resource-type Microsoft.Web/sites \
  --min-count 2 \
  --max-count 10 \
  --count 2
```

Add CPU-based scaling:

```bash
az monitor autoscale rule create \
  --resource-group nuget-feed-rg \
  --autoscale-name nuget-feed-autoscale \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1
```

### Multi-Region Deployment

Deploy to multiple regions with Traffic Manager:

1. Deploy to multiple App Services in different regions
2. Create Traffic Manager profile
3. Add endpoints for each region
4. Configure routing (Performance, Geographic, etc.)

## Security Best Practices

### Enable Managed Identity

```bash
az webapp identity assign \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg
```

Grant database access to managed identity:

```sql
CREATE USER [my-nuget-feed] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [my-nuget-feed];
```

Update connection string to use managed identity:

```
Server=tcp:mynugetfeed-sql.database.windows.net,1433;Database=NuGetFeed;Authentication=Active Directory Managed Identity;
```

### Use Azure Key Vault

Store secrets in Key Vault:

```bash
# Create Key Vault
az keyvault create \
  --name mynugetfeed-kv \
  --resource-group nuget-feed-rg \
  --location eastus

# Add secrets
az keyvault secret set \
  --vault-name mynugetfeed-kv \
  --name SendGridApiKey \
  --value "your-api-key"
```

Reference in App Service:

```bash
az webapp config appsettings set \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --settings \
    Email__SendGrid__ApiKey="@Microsoft.KeyVault(SecretUri=https://mynugetfeed-kv.vault.azure.net/secrets/SendGridApiKey/)"
```

### Enable Application Insights

```bash
az monitor app-insights component create \
  --app nuget-feed-insights \
  --location eastus \
  --resource-group nuget-feed-rg \
  --application-type web

# Get instrumentation key
INSIGHTS_KEY=$(az monitor app-insights component show \
  --app nuget-feed-insights \
  --resource-group nuget-feed-rg \
  --query instrumentationKey -o tsv)

# Configure in App Service
az webapp config appsettings set \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --settings APPINSIGHTS_INSTRUMENTATIONKEY="$INSIGHTS_KEY"
```

## Monitoring and Diagnostics

### Enable Logging

```bash
az webapp log config \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --application-logging filesystem \
  --detailed-error-messages true \
  --failed-request-tracing true \
  --web-server-logging filesystem
```

### View Logs

```bash
az webapp log tail \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg
```

## Cost Optimization

### Right-Size Your Resources

- Start with smaller SKUs and scale up as needed
- Use auto-scaling to match demand
- Review Azure Advisor recommendations

### Use Reserved Instances

For predictable workloads, purchase reserved instances for 1-3 year commitments to save up to 72%.

### Monitor Costs

Set up cost alerts:

```bash
az consumption budget create \
  --budget-name nuget-feed-budget \
  --amount 100 \
  --time-grain Monthly \
  --start-date 2025-01-01 \
  --end-date 2025-12-31 \
  --resource-group nuget-feed-rg
```

## Backup and Disaster Recovery

### Database Backups

Azure SQL automatically backs up databases. Configure retention:

```bash
az sql db ltr-policy set \
  --resource-group nuget-feed-rg \
  --server mynugetfeed-sql \
  --database NuGetFeed \
  --weekly-retention P4W \
  --monthly-retention P12M \
  --yearly-retention P5Y \
  --week-of-year 1
```

### Storage Backups

Enable blob versioning and soft delete:

```bash
az storage account blob-service-properties update \
  --account-name mynugetfeedstorage \
  --enable-versioning true \
  --enable-delete-retention true \
  --delete-retention-days 30
```

## Troubleshooting

### Deployment Failures

Check deployment logs:

```bash
az webapp log deployment show \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg
```

### Database Connection Issues

Test connection from local machine:

```bash
sqlcmd -S mynugetfeed-sql.database.windows.net \
  -d NuGetFeed \
  -U sqladmin \
  -P 'YourPassword123!'
```

### App Service Down

Check status:

```bash
az webapp show \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg \
  --query state -o tsv
```

Restart app:

```bash
az webapp restart \
  --name my-nuget-feed \
  --resource-group nuget-feed-rg
```

## Next Steps

- [Configure Azure AD Integration](azure-ad.md)
- [Set up Storage Options](storage.md)
- [Learn about Customization](../advanced/customization.md)
