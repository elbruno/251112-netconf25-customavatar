# Deployment Guide

This deployment uses .NET 10 runtime.

## Prerequisites

Before deploying, ensure you have:
- [ ] Azure subscription with active credits
- [ ] .NET 10 SDK installed (`dotnet --version`)
- [ ] Aspire workload installed (`dotnet workload install aspire`)
- [ ] Azure Developer CLI installed (for Aspire deployment)
- [ ] Git installed

## Deployment Options

### Option 1: Aspire Deployment with Azure Developer CLI (Recommended)

The easiest and most automated way to deploy is using Azure Developer CLI (azd), which leverages Aspire's built-in provisioning.

#### Step 1: Install Azure Developer CLI

```bash
# Windows (winget)
winget install microsoft.azd

# macOS (Homebrew)
brew tap azure/azd && brew install azd

# Linux
curl -fsSL https://aka.ms/install-azd.sh | bash
```

#### Step 2: Initialize Aspire Project

```bash
# Navigate to AppHost directory
cd dotnet/AzureAIAvatarBlazor.AppHost

# Initialize azd (one-time)
azd init

# When prompted:
# - Environment name: "avatar-demo-prod" (or your choice)
# - Azure location: "westus2" (or your preferred region)
```

#### Step 3: Configure Azure Subscription

```bash
# Login to Azure
azd auth login

# Set target subscription (if you have multiple)
azd config set defaults.subscription "YOUR_SUBSCRIPTION_ID"
```

#### Step 4: Deploy to Azure

```bash
# Deploy everything (provision + deploy)
azd up

# This will:
# ✅ Create resource group
# ✅ Provision Azure OpenAI + deploy model
# ✅ Provision Azure Speech Service
# ✅ Create Azure Container Apps environment
# ✅ Deploy Blazor app as container
# ✅ Configure managed identities
# ✅ Set up networking and DNS
```

**Expected output**:

```
Provisioning Azure resources (azd provision)
  ✓ Provisioned resource group (rg-avatar-demo-prod)
  ✓ Provisioned Azure OpenAI (oai-avatar-demo)
  ✓ Deployed model gpt-4o-mini to Azure OpenAI
  ✓ Provisioned Speech Service (speech-avatar-demo)
  ✓ Provisioned Container Apps environment
  ✓ Configured managed identity

Deploying services (azd deploy)
  ✓ Built container image
  ✓ Pushed to Azure Container Registry
  ✓ Deployed to Azure Container Apps

SUCCESS: Your application is deployed!
  Endpoint: https://azureaiavatarblazor.{random}.azurecontainerapps.io
```

#### Step 5: Verify Deployment

```bash
# Open the deployed app in browser
azd show

# View logs
azd logs

# Monitor the deployment
azd monitor
```

#### Step 6: Update and Redeploy

```bash
# After making code changes
azd deploy

# Or re-provision and deploy
azd up
```

#### Step 7: Cleanup

```bash
# Delete all Azure resources
azd down

# Confirm when prompted
```

### Advanced: Customize Aspire Deployment

You can customize the deployment by editing the generated `infra/` files or by setting environment variables:

```bash
# Set custom avatar character
azd env set Avatar__Character "lisa"

# Set custom deployment name
azd env set OpenAI__DeploymentName "gpt-4o"

# Set system prompt
azd env set SystemPrompt "You are a helpful AI assistant."

# Deploy with custom settings
azd up
```

### Option 2: Azure App Service (Manual)

For manual deployment without Aspire automation:

Azure App Service provides a fully managed platform for hosting web applications.

#### Step 1: Create App Service Resources

```bash
# Set variables
RESOURCE_GROUP="rg-avatar-demo"
LOCATION="westus2"
APP_SERVICE_PLAN="plan-avatar-demo"
WEB_APP_NAME="avatar-demo-$(openssl rand -hex 4)"

# Login to Azure
az login

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan (Linux, B1 tier)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --is-linux \
  --sku B1

# Create Web App
az webapp create \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:9.0"
```

#### Step 2: Configure Application Settings

```bash
# Configure Azure Speech Service
az webapp config appsettings set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AzureSpeech__Region="westus2" \
    AzureSpeech__ApiKey="@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/SpeechApiKey/)"

# Configure Azure OpenAI
az webapp config appsettings set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AzureOpenAI__Endpoint="https://your-resource.openai.azure.com" \
    AzureOpenAI__ApiKey="@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/OpenAIApiKey/)" \
    AzureOpenAI__DeploymentName="gpt-4o-mini"
```

#### Step 3: Build and Publish

```bash
# Navigate to project directory
cd /path/to/customavatarlabs/dotnet/AzureAIAvatarBlazor

# Publish the application
dotnet publish -c Release -o ./publish

# Create deployment ZIP
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deploy \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src-path deploy.zip \
  --type zip
```

#### Step 4: Verify Deployment

```bash
# Get the URL
APP_URL=$(az webapp show --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --query defaultHostName -o tsv)
echo "Application URL: https://$APP_URL"

# Open in browser
open "https://$APP_URL"  # macOS
# or
start "https://$APP_URL"  # Windows
# or
xdg-open "https://$APP_URL"  # Linux
```

### Option 2: Azure Container Apps

For containerized deployments with automatic scaling.

#### Step 1: Create Container Registry

```bash
ACR_NAME="acravatardemo$(openssl rand -hex 4)"

# Create Azure Container Registry
az acr create \
  --name $ACR_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Basic \
  --admin-enabled true
```

#### Step 2: Build and Push Docker Image

Create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AzureAIAvatarBlazor.csproj", "./"]
RUN dotnet restore "AzureAIAvatarBlazor.csproj"
COPY . .
RUN dotnet build "AzureAIAvatarBlazor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureAIAvatarBlazor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzureAIAvatarBlazor.dll"]
```

Build and push:

```bash
# Login to ACR
az acr login --name $ACR_NAME

# Build image
docker build -t $ACR_NAME.azurecr.io/avatar-demo:latest .

# Push image
docker push $ACR_NAME.azurecr.io/avatar-demo:latest
```

#### Step 3: Deploy to Container Apps

```bash
CONTAINER_APP_ENV="env-avatar-demo"
CONTAINER_APP_NAME="app-avatar-demo"

# Create Container Apps environment
az containerapp env create \
  --name $CONTAINER_APP_ENV \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Get ACR credentials
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query passwords[0].value -o tsv)

# Create Container App
az containerapp create \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $CONTAINER_APP_ENV \
  --image $ACR_NAME.azurecr.io/avatar-demo:latest \
  --registry-server $ACR_NAME.azurecr.io \
  --registry-username $ACR_USERNAME \
  --registry-password $ACR_PASSWORD \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3
```

### Option 3: Local Development/Testing

For local development and testing purposes.

```bash
# Navigate to project
cd /path/to/customavatarlabs/dotnet/AzureAIAvatarBlazor

# Set user secrets (one-time)
dotnet user-secrets set "AzureSpeech:Region" "westus2"
dotnet user-secrets set "AzureSpeech:ApiKey" "your-key"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"

# Run application
dotnet run

# Access at https://localhost:5001
```

## Security Configuration

### Using Azure Key Vault

#### Step 1: Create Key Vault

```bash
KEY_VAULT_NAME="kv-avatar-$(openssl rand -hex 4)"

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization false
```

#### Step 2: Add Secrets

```bash
# Add secrets to Key Vault
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "SpeechApiKey" \
  --value "your-speech-api-key"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "OpenAIApiKey" \
  --value "your-openai-api-key"
```

#### Step 3: Grant App Access

```bash
# Get app's managed identity
APP_IDENTITY=$(az webapp identity assign \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Grant access to Key Vault
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $APP_IDENTITY \
  --secret-permissions get list
```

#### Step 4: Update App Settings

```bash
# Reference secrets from Key Vault
az webapp config appsettings set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AzureSpeech__ApiKey="@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/SpeechApiKey/)" \
    AzureOpenAI__ApiKey="@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/OpenAIApiKey/)"
```

## CI/CD Setup

### GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_WEBAPP_NAME: your-app-name
  DOTNET_VERSION: '10.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
      working-directory: ./dotnet/AzureAIAvatarBlazor
    
    - name: Build
      run: dotnet build --no-restore -c Release
      working-directory: ./dotnet/AzureAIAvatarBlazor
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
      working-directory: ./dotnet/AzureAIAvatarBlazor
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./dotnet/AzureAIAvatarBlazor/publish
```

## Monitoring and Diagnostics

### Enable Application Insights

```bash
# Create Application Insights
AI_NAME="ai-avatar-demo"

az monitor app-insights component create \
  --app $AI_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP

# Get instrumentation key
AI_KEY=$(az monitor app-insights component show \
  --app $AI_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey -o tsv)

# Configure app to use Application Insights
az webapp config appsettings set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$AI_KEY"
```

### Enable Logging

```bash
# Enable application logging
az webapp log config \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --application-logging filesystem \
  --level information

# View logs
az webapp log tail --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP
```

## Performance Optimization

### Configure Scaling

```bash
# Enable auto-scaling (requires Standard tier or higher)
az appservice plan update \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku S1

# Configure auto-scale rules
az monitor autoscale create \
  --resource-group $RESOURCE_GROUP \
  --resource $APP_SERVICE_PLAN \
  --resource-type "Microsoft.Web/serverfarms" \
  --name "autoscale-avatar" \
  --min-count 1 \
  --max-count 3 \
  --count 1
```

### Enable CDN (Optional)

```bash
# Create CDN profile
CDN_PROFILE="cdn-avatar-demo"
CDN_ENDPOINT="avatar-demo-$(openssl rand -hex 4)"

az cdn profile create \
  --name $CDN_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_Microsoft

az cdn endpoint create \
  --name $CDN_ENDPOINT \
  --profile-name $CDN_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --origin $WEB_APP_NAME.azurewebsites.net \
  --origin-host-header $WEB_APP_NAME.azurewebsites.net
```

## Cleanup

To remove all resources:

```bash
# Delete resource group (removes all resources)
az group delete --name $RESOURCE_GROUP --yes --no-wait
```

## Troubleshooting

### Common Issues

**Issue**: Application won't start
- Check logs: `az webapp log tail --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP`
- Verify .NET version matches runtime
- Check configuration settings

**Issue**: Can't connect to Azure services
- Verify API keys are correct
- Check network security groups
- Verify service endpoints

**Issue**: High latency
- Enable CDN for static content
- Configure auto-scaling
- Check Azure service regions (co-locate resources)

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure Container Apps Documentation](https://docs.microsoft.com/azure/container-apps/)
- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [GitHub Actions for Azure](https://github.com/Azure/actions)
