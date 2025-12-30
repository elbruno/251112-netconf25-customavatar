# Quick Start Guide

Get up and running with the Azure AI Avatar Blazor application in minutes using .NET Aspire!

## 🚀 5-Minute Setup

### Step 1: Prerequisites Check

Verify you have the required software:

```bash
# Check .NET version (should be 10.0 or higher)
dotnet --version

# Check git
git --version

# Install Aspire workload
dotnet workload install aspire
```

If you don't have .NET 10:
- **Windows/macOS/Linux**: Download from https://dotnet.microsoft.com/download/dotnet/10.0

### Step 2: Clone and Navigate

```bash
# Clone the repository
git clone https://github.com/elbruno/customavatarlabs.git

# Navigate to the AppHost project (Aspire orchestrator)
cd customavatarlabs/dotnet/AzureAIAvatarBlazor.AppHost
```

### Step 3: Configure Credentials

With Aspire, all credentials are managed by the AppHost. Choose your configuration method:

#### Method A: AppHost User Secrets (Recommended for Development)

```bash
# Navigate to AppHost project (if not already there)
cd dotnet/AzureAIAvatarBlazor.AppHost

# (Optional) Configure Application Insights connection string for telemetry
dotnet user-secrets set "ConnectionStrings:appinsights" "InstrumentationKey=YOUR_INSTRUMENTATION_KEY;IngestionEndpoint=https://YOUR_REGION.in.applicationinsights.azure.com/;LiveEndpoint=https://YOUR_REGION.livediagnostics.monitor.azure.com/"

# (Optional) Configure Microsoft Foundry project endpoint
dotnet user-secrets set "ConnectionStrings:microsoftfoundryproject" "https://YOUR_FOUNDRY_PROJECT.services.ai.azure.com/api/projects/YOUR_PROJECT_ID"

# (Optional) Configure Azure Tenant ID for Microsoft Foundry
dotnet user-secrets set "ConnectionStrings:tenantId" "YOUR_TENANT_ID"

# Set application defaults
dotnet user-secrets set "Avatar:Character" "lisa"
dotnet user-secrets set "Avatar:Style" "casual-sitting"
dotnet user-secrets set "SystemPrompt" "You are a helpful AI assistant."
```

Replace:
- `YOUR_INSTRUMENTATION_KEY`: Your Application Insights instrumentation key (optional)
- `YOUR_REGION`: Your Application Insights region (e.g., westus2)
- `YOUR_FOUNDRY_PROJECT`: Your Microsoft Foundry project name (optional)
- `YOUR_PROJECT_ID`: Your Microsoft Foundry project ID (optional)
- `YOUR_TENANT_ID`: Your Azure tenant ID (optional)

**Important**: 
- **Application Insights** (optional): If not provided in development, telemetry will only be visible in the Aspire Dashboard. In production, Aspire automatically provisions Application Insights.
- **Microsoft Foundry** (optional): If provided, the application will use Microsoft Foundry agents and IChatClient. If not provided, the application will fall back to direct Azure OpenAI configuration.
- All connection strings use the Aspire connection string format.


#### Method B: Environment Variables (CI/CD & Production)

```powershell
# Windows PowerShell
$env:ConnectionStrings__openai = "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_OPENAI_API_KEY;"
$env:ConnectionStrings__speech = "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_SPEECH_API_KEY;"
$env:Avatar__Character = "lisa"
$env:OpenAI__DeploymentName = "gpt-4o-mini"
```

**macOS/Linux (Bash)**:
```bash
# Use double underscores for nested configuration
export ConnectionStrings__openai="Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_OPENAI_API_KEY;"
export ConnectionStrings__speech="Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_SPEECH_API_KEY;"
export Avatar__Character="lisa"
export OpenAI__DeploymentName="gpt-4o-mini"
```

#### Method C: Azure Provisioning (Production - Automatic)

For production deployments, Aspire can automatically provision Azure resources:

```bash
cd dotnet/AzureAIAvatarBlazor.AppHost

# Configure Azure subscription (one-time)
dotnet user-secrets set "Azure:SubscriptionId" "YOUR_SUBSCRIPTION_ID"
dotnet user-secrets set "Azure:ResourceGroupPrefix" "rg-avatar"
dotnet user-secrets set "Azure:Location" "westus2"

# Deploy using Azure Developer CLI
azd init
azd up
```

Aspire will automatically:
- ✅ Create Azure OpenAI resource
- ✅ Create Azure Speech Service resource
- ✅ Deploy GPT-4o-mini model
- ✅ Configure all connection strings
- ✅ Set up managed identities
- ✅ Provision Application Insights for monitoring

### Step 4: Run the Application

With Aspire, you run from the AppHost project, which orchestrates all services:

#### Option A: VS Code (Recommended)

1. Open the repository in VS Code
2. Press **Ctrl+Shift+B** (or Cmd+Shift+B on macOS)
3. Or: Press **Ctrl+Shift+P** → "Tasks: Run Task" → "Aspire: Run"

#### Option B: Command Line

```bash
# From the AppHost directory
cd dotnet/AzureAIAvatarBlazor.AppHost
dotnet run
```

#### Option C: Visual Studio 2022

1. Open `dotnet/AzureAIAvatarBlazor.slnx`
2. Set `AzureAIAvatarBlazor.AppHost` as startup project
3. Press **F5** to run with debugging

You'll see output similar to:

```
Building...
info: Aspire.Hosting[0]
      Aspire Dashboard listening at: https://localhost:15216
info: Aspire.Hosting[0]
      azureaiavatarblazor listening at: https://localhost:5001
```

### Step 5: Access the Application

Two important URLs will be available:

1. **Aspire Dashboard**: https://localhost:15216
   - Monitor all services
   - View logs and metrics
   - Check resource status
   - Distributed tracing
   - **Custom telemetry**: View avatar sessions, AI response times, and chat metrics
   - **Application Insights**: If configured, telemetry is also sent to Azure

2. **Blazor Application**: https://localhost:5001
   - The main avatar chat interface

## 🎯 First Steps in the Application

### 1. Home Page

When you first open the application, you'll see:
- **Welcome screen** with feature overview
- **Two main options**: Start Chat or Configure

### 2. Configuration (Optional but Recommended)

Click **"Configure"** to verify and customize settings:

1. **Azure OpenAI Configuration**
   - **Note**: Microsoft Foundry endpoint and Application Insights are managed by Aspire AppHost (not editable in UI)
   - System Prompt: Configure how the AI assistant responds
   - Deployment Name: AI model deployment (e.g., gpt-4o-mini)
   - Agent Name: Optional agent name for Microsoft Foundry
   
2. **Azure Speech Service**
   - Region should match your resource
   - API key is already set from secrets
   
3. **Avatar Settings**
   - Choose avatar character (Lisa, Harry, Jeff, etc.)
   - Select style (casual-sitting, business, formal)
   - Adjust audio gain if needed
   
4. Click **"Save Configuration"**

> **💡 Configuration Note**: The application now uses Microsoft Foundry for AI operations. The Microsoft Foundry endpoint, Application Insights connection string, and Azure Tenant ID are managed by Aspire AppHost through connection strings and are NOT editable in the Configuration UI. This ensures secure management of credentials and endpoints.

### 3. Start Chatting

1. Click **"Back to Chat"** or navigate to **"Chat"**
2. Click **"Open Avatar Session"**
   - Wait for the avatar video to load
   - You'll see the avatar appear in the video panel
3. Enable **"Type Message"** checkbox
4. Type a message and press **Enter** or click **"Send"**
5. Watch the avatar respond with voice and lip sync!

## 🎨 Customization Tips

### Change Avatar

In Config page:
```
Character: Lisa
Style: casual-sitting
```

Try different combinations:
- **Harry**: business, casual, youthful
- **Lisa**: casual-sitting
- **Lori**: casual, formal, graceful

### Adjust System Prompt

Customize how the AI responds:

**Friendly Assistant**:
```
You are a friendly and helpful AI assistant. You provide clear, 
concise answers and always maintain a positive tone.
```

**Technical Expert**:
```
You are a technical expert specializing in software development. 
Provide detailed, accurate answers with code examples when relevant.
```

**Language Tutor**:
```
You are a language tutor. Help users learn new languages by providing
translations, explanations, and encouraging practice.
```

### Adjust Voice Volume

If the avatar voice is too quiet or loud:

1. Go to **Config**
2. Scroll to **Avatar Configuration**
3. Adjust **Audio Gain** slider (0.1x - 5.0x)
   - **< 1.0**: Quieter
   - **= 1.0**: Normal
   - **> 1.0**: Louder (recommended: 1.5-2.0)

### Multiple Languages

Update STT Locales in Config:
```
en-US,es-ES,fr-FR,de-DE,it-IT,ja-JP,ko-KR,zh-CN
```

The avatar will automatically detect the input language!

### View Telemetry and Monitoring

The application includes comprehensive telemetry tracking via Application Insights integration:

#### Aspire Dashboard (Local Development)

1. Open the **Aspire Dashboard**: https://localhost:15216
2. Navigate to different tabs:
   - **Logs**: View structured logs from all services
   - **Metrics**: See custom metrics like:
     - `avatar.sessions.started`: Number of avatar sessions
     - `chat.messages.sent`: Chat message counts
     - `ai.response.duration`: AI response times
     - `avatar.session.duration`: Session durations
   - **Traces**: View distributed traces for operations like:
     - `GetChatCompletion`: Full AI request/response lifecycle
     - `TestConnection`: Connection validation traces
   - **Resources**: Check health and status of all services

#### Custom Telemetry Events

The application tracks:
- 🎭 **Avatar Operations**: Session start/end, character selection
- 💬 **Chat Interactions**: Message counts, role tracking
- 🤖 **AI Performance**: Response times, token counts (when available)
- ⚙️ **Configuration Changes**: Character, mode, settings changes
- 🔊 **Speech Synthesis**: Voice selection, synthesis duration
- 🌐 **WebRTC Status**: Connection health

#### Application Insights (Production)

If you configured an Application Insights connection string:

1. Open **Azure Portal**: https://portal.azure.com
2. Navigate to your **Application Insights** resource
3. View:
   - **Application Map**: Service dependencies
   - **Performance**: Request/response times
   - **Failures**: Exceptions and errors
   - **Metrics**: Custom metrics (same as Aspire Dashboard)
   - **Logs**: Query structured logs with KQL

**Example KQL Query** (in Application Insights Logs):
```kql
traces
| where message contains "Avatar session"
| project timestamp, message, customDimensions
| order by timestamp desc
```

## 🔧 Troubleshooting

### "Azure Speech credentials not configured"

**Solution**: Set up your Speech Service credentials:
```bash
dotnet user-secrets set "AzureSpeech:Region" "westus2"
dotnet user-secrets set "AzureSpeech:ApiKey" "YOUR_KEY"
```

### "Failed to get avatar token"

**Possible causes**:
1. **Wrong region**: Check your Speech resource region in Azure Portal
2. **Invalid API key**: Verify the key is correct and not expired
3. **Network issue**: Check internet connection

**Solution**:
```bash
# Verify credentials
az cognitiveservices account show --name YOUR_SPEECH_RESOURCE --resource-group YOUR_RG
```

### Avatar video won't load

**Check**:
1. Browser supports WebRTC (Chrome, Edge, Firefox, Safari)
2. No firewall blocking WebRTC
3. Check browser console for errors (F12)

**Try**:
- Use Chrome or Edge (best WebRTC support)
- Disable browser extensions temporarily
- Try incognito/private mode

### Chat not responding

**Check**:
1. Azure OpenAI credentials are correct
2. Model deployment is active
3. Not exceeding rate limits

**Solution**:
```bash
# Test OpenAI connection
curl -X POST "https://YOUR_RESOURCE.openai.azure.com/openai/deployments/YOUR_MODEL/chat/completions?api-version=2024-02-01" \
  -H "api-key: YOUR_KEY" \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hello"}]}'
```

### Port already in use

**Error**: `Failed to bind to address http://127.0.0.1:5000`

**Solution**:
```bash
# Run on different port
dotnet run --urls "http://localhost:5100;https://localhost:5101"
```

## 📚 Next Steps

Now that you're up and running:

1. **Explore Features**
   - Try different avatars and styles
   - Experiment with system prompts
   - Test multiple languages

2. **Read Documentation**
   - [Full README](../README.md) - Complete feature list
   - [Architecture](./ARCHITECTURE.md) - Technical details
   - [Deployment](./DEPLOYMENT.md) - Production deployment

3. **Customize**
   - Modify UI in Blazor components
   - Add new features to services
   - Extend configuration options

4. **Deploy**
   - Deploy to Azure App Service
   - Set up CI/CD pipeline
   - Enable monitoring and logging

## 🆘 Getting Help

If you're stuck:

1. **Check logs**: Look at console output for errors
2. **Browser console**: Press F12 and check for JavaScript errors
3. **Review docs**: Check the documentation files
4. **GitHub Issues**: Search or create an issue
5. **Azure Support**: For Azure-specific issues

## 🎉 Success!

You've successfully set up the Azure AI Avatar Blazor application!

Enjoy exploring the power of Azure AI Services with .NET 10 and Blazor! 🚀
