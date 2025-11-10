# Azure AI Avatar - Aspire Migration Summary

## 📊 Quick Overview

**Goal**: Modernize the .NET Blazor application to use .NET Aspire for centralized AI resource management and eliminate dependency on appsettings.json files.

**Status**: Plan Complete ✅  
**Estimated Effort**: 5-6 days  
**Risk Level**: Medium (rollback strategy included)

---

## 🎯 Key Objectives

1. ✅ **Centralize configuration** in AppHost (no more appsettings.json in Blazor app)
2. ✅ **Add Aspire AI resources** (OpenAI, Speech Service, Cognitive Search)
3. ✅ **Use latest packages** (Aspire 9.5.2 stable + 9.5.2-preview.1 for OpenAI client)
4. ✅ **Update all documentation** to reflect new architecture
5. ✅ **Add Aspire CLI task** for VS Code integration
6. ✅ **Enable automatic Azure provisioning** for production deployments

---

## 📦 Package Changes

### AppHost (NEW Packages)

```xml
<PackageReference Include="Aspire.Hosting.AppHost" Version="9.5.2" />
<PackageReference Include="Aspire.Hosting.Azure.CognitiveServices" Version="9.5.2" />
<PackageReference Include="Aspire.Hosting.Azure.Search" Version="9.5.2" />
```

### Blazor App (UPDATED)

```xml
<!-- REMOVED -->
<!-- <PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" /> -->
<!-- <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.10" /> -->

<!-- ADDED -->
<PackageReference Include="Aspire.Azure.AI.OpenAI" Version="9.5.2-preview.1.25522.3" />

<!-- KEPT (no Aspire component yet) -->
<PackageReference Include="Microsoft.CognitiveServices.Speech" Version="1.46.0" />
<PackageReference Include="Azure.Search.Documents" Version="11.7.0" />
```

---

## 🏗️ Architecture Changes

### Before (Current)

```
Blazor App
  ├── appsettings.json (credentials)
  ├── appsettings.Development.json (local overrides)
  ├── User Secrets (dev only)
  └── Manual Azure SDK client instantiation
```

### After (Aspire)

```
AppHost (Orchestrator)
  ├── User Secrets (dev credentials)
  ├── Azure Provisioning (prod)
  └── Injects:
      ├── Connection Strings (openai, speech, search)
      └── Environment Variables (Avatar, SystemPrompt)
          ↓
      Blazor App
        ├── NO appsettings.json files
        ├── Aspire-managed clients (AzureOpenAIClient)
        └── Services read from IConfiguration (env vars)
```

---

## 🔧 Configuration Migration

### Old Way (appsettings.json)

```json
{
  "AzureSpeech": {
    "Region": "westus2",
    "ApiKey": "YOUR_KEY"
  },
  "AzureOpenAI": {
    "Endpoint": "https://...",
    "ApiKey": "YOUR_KEY",
    "DeploymentName": "gpt-4o-mini"
  }
}
```

### New Way (AppHost User Secrets)

```powershell
cd dotnet/AzureAIAvatarBlazor.AppHost

dotnet user-secrets set "ConnectionStrings:openai" "Endpoint=https://{resource}.openai.azure.com/;Key={key};"
dotnet user-secrets set "ConnectionStrings:speech" "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key={key};"
dotnet user-secrets set "OpenAI:DeploymentName" "gpt-4o-mini"
dotnet user-secrets set "Avatar:Character" "lisa"
```

---

## 📝 Files to Modify

### Phase 1: AppHost

- ✅ `dotnet/AzureAIAvatarBlazor.AppHost/AzureAIAvatarBlazor.AppHost.csproj` (add packages)
- ✅ `dotnet/AzureAIAvatarBlazor.AppHost/AppHost.cs` (add AI resources)
- ✅ `dotnet/AzureAIAvatarBlazor.AppHost/appsettings.Development.json` (optional config)

### Phase 2: Blazor App

- ✅ `dotnet/AzureAIAvatarBlazor/AzureAIAvatarBlazor.csproj` (update packages)
- ✅ `dotnet/AzureAIAvatarBlazor/Program.cs` (add Aspire client)
- ✅ `dotnet/AzureAIAvatarBlazor/Services/AzureOpenAIService.cs` (use injected client)
- ✅ `dotnet/AzureAIAvatarBlazor/Services/AzureSpeechService.cs` (parse connection strings)
- ✅ `dotnet/AzureAIAvatarBlazor/Services/ConfigurationService.cs` (remove appsettings logic)

### Phase 3: Configuration

- ❌ DELETE: `dotnet/AzureAIAvatarBlazor/appsettings.json`
- ❌ DELETE: `dotnet/AzureAIAvatarBlazor/appsettings.Development.json`
- ✅ UPDATE: `.gitignore` (protect AppHost secrets)

### Phase 4: Documentation

- ✅ `dotnet/docs/QUICKSTART.md` (AppHost secrets instructions)
- ✅ `dotnet/docs/ARCHITECTURE.md` (add Aspire orchestration diagram)
- ✅ `dotnet/docs/DEPLOYMENT.md` (add `azd up` instructions)
- ✅ `README.md` (update prerequisites)

### Phase 5: VS Code Integration

- ✅ `.vscode/tasks.json` (add Aspire CLI tasks)
- ✅ `.vscode/launch.json` (optional debug config)

---

## 🚀 Running the App

### Development (Local)

```powershell
# Option 1: VS Code Task
# Press Ctrl+Shift+P → "Tasks: Run Task" → "Aspire: Run with Dashboard"

# Option 2: Command Line
cd dotnet/AzureAIAvatarBlazor.AppHost
dotnet run

# Option 3: Aspire CLI
aspire run --project dotnet/AzureAIAvatarBlazor.AppHost/AzureAIAvatarBlazor.AppHost.csproj
```

**Access**:

- Aspire Dashboard: `https://localhost:15216`
- Blazor App: `https://localhost:5001`

### Production (Azure)

```powershell
# One-time setup
winget install microsoft.azd
cd dotnet/AzureAIAvatarBlazor.AppHost
azd init

# Deploy
azd up

# Automatic:
# ✅ Creates Azure OpenAI + deploys model
# ✅ Creates Azure Speech Service
# ✅ Creates Container Apps environment
# ✅ Deploys Blazor app as container
# ✅ Configures managed identities
```

---

## 🧪 Testing Checklist

After implementation, verify:

### Local Development

- [ ] `dotnet run` from AppHost starts successfully
- [ ] Aspire Dashboard opens at `https://localhost:15216`
- [ ] Blazor app accessible at `https://localhost:5001`
- [ ] Dashboard shows configured connection strings
- [ ] Avatar session connects without errors
- [ ] Chat messages stream from Azure OpenAI
- [ ] No errors about missing configuration

### VS Code Integration

- [ ] Task "Aspire: Run with Dashboard" works
- [ ] Task "Aspire: Stop" terminates processes
- [ ] No PowerShell execution policy errors

### Configuration

- [ ] No `appsettings.json` files in Blazor app
- [ ] `dotnet user-secrets list` shows AppHost secrets
- [ ] Services read from environment variables
- [ ] Aspire connection string parsing works

### Production Deployment

- [ ] `azd up` provisions Azure resources
- [ ] Azure OpenAI model deployed automatically
- [ ] Container app runs successfully
- [ ] Managed identity authentication works
- [ ] No manual Azure Portal steps required

---

## 🛡️ Rollback Plan

If migration fails:

```powershell
# 1. Restore appsettings.json files
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/appsettings.json
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/appsettings.Development.json

# 2. Restore original NuGet packages
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/AzureAIAvatarBlazor.csproj

# 3. Restore original services
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/Program.cs
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/Services/

# 4. Rebuild
dotnet restore
dotnet build
dotnet run
```

---

## 📚 Key References

- **Detailed Plan**: [`ASPIRE-MIGRATION-PLAN.md`](./ASPIRE-MIGRATION-PLAN.md)
- **Aspire CLI Docs**: <https://learn.microsoft.com/dotnet/aspire/cli/overview>
- **Azure Developer CLI**: <https://learn.microsoft.com/azure/developer/azure-developer-cli/overview>
- **Aspire Azure AI**: <https://learn.microsoft.com/dotnet/aspire/azureai/azureai-openai-component>
- **NuGet Packages**:
  - <https://www.nuget.org/packages/Aspire.Hosting.Azure.CognitiveServices>
  - <https://www.nuget.org/packages/Aspire.Azure.AI.OpenAI>

---

## 🎓 What You'll Learn

1. **Aspire Orchestration**: How to define and manage Azure AI resources in code
2. **Configuration Management**: Modern secrets handling with connection strings
3. **Dependency Injection**: Using Aspire-managed clients instead of manual instantiation
4. **Azure Provisioning**: Automatic resource creation with `azd up`
5. **Telemetry**: Built-in OpenTelemetry with Aspire Dashboard
6. **VS Code Integration**: Custom tasks for Aspire CLI

---

## ✅ Success Metrics

- **Configuration**: Zero hardcoded credentials or endpoints
- **Code Quality**: All services use DI, no manual client creation
- **Developer Experience**: Single command to run entire stack (`aspire run`)
- **Production Ready**: One-command deployment (`azd up`)
- **Documentation**: All docs reflect new Aspire architecture
- **Backward Compatibility**: Supports old env var names (AZURE_SPEECH_REGION)

---

## 🎯 Next Steps

1. **Review**: Read full plan in [`ASPIRE-MIGRATION-PLAN.md`](./ASPIRE-MIGRATION-PLAN.md)
2. **Prerequisites**: Install Aspire workload and CLI
3. **Backup**: Create git branch for migration work
4. **Phase 1**: Start with AppHost updates
5. **Test**: Verify each phase before proceeding
6. **Deploy**: Test production deployment with `azd up`

---

**Ready to start? See [`ASPIRE-MIGRATION-PLAN.md`](./ASPIRE-MIGRATION-PLAN.md) for detailed implementation steps.**
