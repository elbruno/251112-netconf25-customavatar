# Getting Started with Aspire Migration

## 🚀 Quick Start (15 Minutes)

This guide gets you from zero to running the migrated app in 15 minutes.

---

## Prerequisites Check

Run these commands to verify you have everything:

```powershell
# Check .NET 9 SDK
dotnet --version
# Expected: 9.0.100 or higher

# Check Aspire workload
dotnet workload list
# Expected: "aspire" in the list

# Check Aspire CLI
aspire --version
# Expected: Any version (if installed)
```

### Missing Prerequisites?

```powershell
# Install .NET 9 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0

# Install Aspire workload
dotnet workload install aspire

# Install Aspire CLI
dotnet tool install -g aspire

# Verify installations
dotnet workload list
aspire --version
```

---

## Step 1: Get Your Azure Credentials (5 min)

You need these values from Azure Portal:

### Azure OpenAI

1. Go to <https://portal.azure.com>
2. Navigate to your Azure OpenAI resource
3. Copy:
   - **Endpoint**: `https://your-resource.openai.azure.com/`
   - **Key**: From "Keys and Endpoint" page
   - **Deployment Name**: Your deployed model (e.g., "gpt-4o-mini")

### Azure Speech Service

1. Navigate to your Speech Service resource
2. Copy:
   - **Region**: e.g., "westus2"
   - **Key**: From "Keys and Endpoint" page

**Don't have resources yet?**

- [Create Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource)
- [Create Speech Service](https://learn.microsoft.com/azure/ai-services/speech-service/overview#create-the-azure-resource)

---

## Step 2: Configure AppHost Secrets (5 min)

```powershell
# Navigate to AppHost project
cd dotnet/AzureAIAvatarBlazor.AppHost

# Configure Azure OpenAI
dotnet user-secrets set "ConnectionStrings:openai" "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_KEY;"

# Configure Azure Speech
dotnet user-secrets set "ConnectionStrings:speech" "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"

# Set application defaults
dotnet user-secrets set "OpenAI:DeploymentName" "YOUR_DEPLOYMENT_NAME"
dotnet user-secrets set "Avatar:Character" "lisa"
dotnet user-secrets set "Avatar:Style" "casual-sitting"
dotnet user-secrets set "SystemPrompt" "You are a helpful AI assistant with a friendly personality."

# Verify secrets saved
dotnet user-secrets list
```

**Expected output**:

```
ConnectionStrings:openai = Endpoint=https://...
ConnectionStrings:speech = Endpoint=https://...
OpenAI:DeploymentName = gpt-4o-mini
Avatar:Character = lisa
...
```

---

## Step 3: Run the App (5 min)

### Option A: Command Line (Fastest)

```powershell
# From AppHost directory
dotnet run
```

**Expected output**:

```
Building...
info: Aspire.Hosting[0]
      Aspire Dashboard listening at: https://localhost:15216
info: Aspire.Hosting[0]
      azureaiavatarblazor listening at: https://localhost:5001
```

### Option B: VS Code Task

1. Press `Ctrl+Shift+B` (or `Ctrl+Shift+P` → "Tasks: Run Task")
2. Select "Aspire: Run with Dashboard"
3. Wait for dashboard to open automatically

### Option C: Aspire CLI

```powershell
aspire run --project dotnet/AzureAIAvatarBlazor.AppHost/AzureAIAvatarBlazor.AppHost.csproj --dashboard
```

---

## Step 4: Verify Everything Works

### ✅ Check Aspire Dashboard

Open <https://localhost:15216>

**Resources Tab**:

- [ ] "azureaiavatarblazor" shows as "Running"
- [ ] "openai" connection string configured
- [ ] "speech" connection string configured

**Console Logs Tab**:

- [ ] Select "azureaiavatarblazor"
- [ ] See "Application started" message
- [ ] No error messages

### ✅ Check Blazor App

Open <https://localhost:5001>

**Home Page**:

- [ ] Page loads without errors
- [ ] Navigation menu visible
- [ ] Click "Chat" to go to chat page

**Chat Page**:

1. [ ] Click "Open Avatar Session"
2. [ ] Avatar video loads in video player
3. [ ] Enable "Type Message" checkbox
4. [ ] Type "Hello, how are you?" and press Enter
5. [ ] Avatar speaks and responds
6. [ ] Response streams in chat area

**Config Page**:

1. [ ] Click "Configure" in nav menu
2. [ ] Azure Speech region shows correctly
3. [ ] Azure OpenAI endpoint shows correctly
4. [ ] Avatar character shows "lisa"
5. [ ] No "missing configuration" warnings

---

## 🎉 Success

If all checks passed, your migration is working! Here's what you have now:

✅ **Centralized Configuration**: All secrets in AppHost user secrets  
✅ **Aspire Orchestration**: Automatic connection string injection  
✅ **Telemetry Dashboard**: Real-time logs, metrics, and traces  
✅ **Single Command Start**: `dotnet run` or `Ctrl+Shift+B`  
✅ **Production Ready**: Can deploy with `azd up`

---

## 🐛 Troubleshooting

### Problem: "Command 'aspire' not found"

```powershell
dotnet tool install -g aspire
```

### Problem: "Failed to bind to address"

Port already in use. Stop existing processes:

```powershell
Get-Process -Id (Get-NetTCPConnection -LocalPort 15216 -ErrorAction SilentlyContinue).OwningProcess | Stop-Process -Force
Get-Process -Id (Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue).OwningProcess | Stop-Process -Force
```

### Problem: "Azure OpenAI credentials not configured"

Check your user secrets:

```powershell
cd dotnet/AzureAIAvatarBlazor.AppHost
dotnet user-secrets list
```

Expected to see `ConnectionStrings:openai` with your endpoint and key.

### Problem: Avatar won't connect

1. Check Aspire Dashboard → Console Logs for errors
2. Open browser console (F12) for JavaScript errors
3. Verify Speech Service credentials in secrets
4. Check that region matches your Speech resource

### Problem: Chat doesn't respond

1. Verify OpenAI deployment name is correct
2. Check Aspire Dashboard → Traces for failed HTTP requests
3. Verify model is deployed in Azure Portal
4. Check rate limits and quota in Azure Portal

---

## 📚 What's Next?

Now that the basic migration is working, explore:

### Immediate Next Steps

1. **Read the full plan**: [`ASPIRE-MIGRATION-PLAN.md`](./ASPIRE-MIGRATION-PLAN.md)
2. **Review architecture**: [`dotnet/docs/ARCHITECTURE.md`](./dotnet/docs/ARCHITECTURE.md)
3. **Customize settings**: Try different avatars, prompts, voices

### Production Deployment

```powershell
# Install Azure Developer CLI
winget install microsoft.azd

# Initialize and deploy
cd dotnet/AzureAIAvatarBlazor.AppHost
azd init
azd up
```

See [`ASPIRE-MIGRATION-PLAN.md`](./ASPIRE-MIGRATION-PLAN.md) Phase 6 for details.

### Advanced Features

- **Add Redis**: For session state management
- **Add SQL**: For chat history persistence
- **Managed Identity**: Remove API keys in production
- **Custom Domains**: Map to your own domain
- **CI/CD**: GitHub Actions with `azd deploy`

---

## 🆘 Need Help?

1. **Check logs**: Aspire Dashboard → Console Logs
2. **Review docs**:
   - [Migration Plan](./ASPIRE-MIGRATION-PLAN.md)
   - [Roadmap](./MIGRATION-ROADMAP.md)
   - [Tasks Reference](./ASPIRE-TASKS-REFERENCE.md)
3. **Search issues**: [GitHub Issues](https://github.com/elbruno/customavatarlabs/issues)
4. **Ask community**:
   - [.NET Discord](https://aka.ms/dotnet-discord)
   - [Aspire GitHub Discussions](https://github.com/dotnet/aspire/discussions)

---

## 📊 Migration Status

Track your progress:

- [x] Prerequisites installed
- [x] Azure credentials obtained
- [x] AppHost secrets configured
- [x] App running locally
- [x] Aspire Dashboard working
- [x] Avatar connects successfully
- [x] Chat functionality verified
- [ ] Documentation updated (if implementing full migration)
- [ ] VS Code tasks configured (if implementing full migration)
- [ ] Production deployment tested (optional)

---

**Congratulations!** 🎉 You've successfully migrated to .NET Aspire!

For the complete implementation guide, see [`ASPIRE-MIGRATION-PLAN.md`](./ASPIRE-MIGRATION-PLAN.md).
