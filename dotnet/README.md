# Azure AI Avatar - .NET 9 Blazor with Aspire

A modern, cloud-native Blazor application powered by .NET Aspire that demonstrates Azure AI capabilities including Speech Services (STT/TTS with Talking Avatar) and Azure OpenAI for intelligent conversations.

## 🎯 Overview

This is the **.NET 9 Blazor implementation** of the Azure AI Avatar demo, featuring:

- **Cloud-Native Architecture** with .NET Aspire orchestration
- **Enterprise-Grade Features** including OpenTelemetry, health checks, and resilience patterns
- **Automatic Azure Provisioning** with Azure Developer CLI (`azd up`)
- **Production-Ready** with managed identities and secure secret management
- **Developer Experience** optimized with Aspire Dashboard and VS Code integration

## 🤖 Three Operational Modes

This application supports **three different modes** for AI conversations, each with different capabilities and use cases:

### Mode 1: Chat with LLM (Default)

**Direct integration with Azure OpenAI**

- Uses standard Azure OpenAI chat completion API
- Simple request/response pattern with streaming support
- Best for: Quick demos, simple conversations, direct LLM access
- Configuration: Requires Azure OpenAI endpoint, API key, and deployment name

### Mode 2: Chat with Agent-LLM

**Microsoft Agent Framework with Azure OpenAI**

- Uses [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/) with standard LLM
- Enables agent-based workflows and orchestration patterns
- Supports tool calling, memory, and advanced agent behaviors
- Best for: Complex workflows, multi-step reasoning, tool integration
- Configuration: Same as Mode 1 + Agent Framework settings

### Mode 3: Chat with Azure AI Foundry Agent

**Pre-built agents from Azure AI Foundry**

- Connects to agents already created in [Azure AI Foundry](https://ai.azure.com)
- Uses Microsoft Agent Framework with pre-configured agent endpoints
- Supports grounding, RAG (Retrieval Augmented Generation), and custom tools
- Best for: Production scenarios, enterprise agents, grounded responses
- Configuration: Requires AI Foundry project ID, agent ID, and managed identity/key
- Learn more: [Azure AI Foundry Agent Types](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/azure-ai-foundry-agent?pivots=programming-language-csharp)

> **💡 Tip**: Switch between modes in the Configuration page (`/config`) under "Azure OpenAI / Agent Configuration" → "Mode" dropdown

## 🚀 Features

### Core Capabilities

- ✅ **Three AI Modes**: Direct LLM, Agent-LLM, or Azure AI Foundry Agent
- ✅ **Interactive Chat**: Real-time conversations with AI-powered avatars
- ✅ **Speech-to-Text (STT)**: Multi-language voice input recognition
- ✅ **Text-to-Speech (TTS)**: Natural-sounding voice synthesis  
- ✅ **Talking Avatar**: WebRTC-based video streaming with synchronized lip movement
- ✅ **Microsoft Agent Framework**: Advanced agent workflows with tool calling
- ✅ **Azure AI Foundry Integration**: Connect to pre-built enterprise agents
- ✅ **Custom Avatars**: Support for custom avatar characters and styles
- ✅ **Multi-language Support**: Automatic language detection for 40+ languages

### Enterprise Features (.NET Aspire)

- ✅ **Centralized Orchestration**: Single AppHost manages all resources
- ✅ **No Configuration Files**: Zero appsettings.json files in application
- ✅ **Built-in Telemetry**: OpenTelemetry with logs, metrics, and distributed tracing
- ✅ **Service Discovery**: Automatic service registration and discovery
- ✅ **Health Checks**: Built-in health monitoring and dashboards
- ✅ **Resilience Patterns**: Automatic retries, circuit breakers, and timeouts
- ✅ **One-Command Deployment**: Deploy to Azure with `azd up`
- ✅ **Aspire Dashboard**: Real-time monitoring at <https://localhost:15216>

## 📋 Prerequisites

### Required Software

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** or later
2. **.NET Aspire Workload**:

   ```bash
   dotnet workload install aspire
   ```

3. **Modern Browser**: Chrome, Edge, Firefox, or Safari
4. **IDE** (Recommended): Visual Studio 2022 or VS Code

### Required Azure Resources

**For All Modes:**

1. **[Azure Speech Service](https://portal.azure.com/#create/Microsoft.CognitiveServicesSpeechServices)**
   - Note your API Key and Region (e.g., `westus2`)
   - Avatar feature must be enabled

**For Mode 1 (LLM) and Mode 2 (Agent-LLM):**
2. **[Azure OpenAI Service](https://portal.azure.com/#create/Microsoft.CognitiveServicesOpenAI)**

- Deploy a chat model (e.g., `gpt-4o-mini`, `gpt-4`)
- Note your Endpoint, API Key, and Deployment Name

**For Mode 3 (Azure AI Foundry Agent):**
3. **[Azure AI Foundry](https://ai.azure.com)** Project

- Create or select an existing AI Foundry project
- Create and deploy an agent in the AI Foundry portal
- Note your Project Connection String and Agent ID
- Configure authentication (Managed Identity or API Key)

**Optional for All Modes:**
4. **Azure Cognitive Search** (for grounded responses/"On Your Data")

- Create and populate a search index
- Note Endpoint, API Key, and Index Name

## 🚀 Quick Start

### 5-Minute Setup

```bash
# 1. Clone the repository
git clone https://github.com/elbruno/customavatarlabs.git
cd customavatarlabs/dotnet

# 2. Install Aspire workload (if not already installed)
dotnet workload install aspire

# 3. Configure AppHost secrets
cd AzureAIAvatarBlazor.AppHost

# Required for all modes
dotnet user-secrets set "ConnectionStrings:speech" "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_SPEECH_KEY;"
dotnet user-secrets set "Avatar:Character" "lisa"

# For Mode 1 (LLM) or Mode 2 (Agent-LLM)
dotnet user-secrets set "ConnectionStrings:openai" "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_OPENAI_KEY;"
dotnet user-secrets set "OpenAI:DeploymentName" "gpt-4o-mini"
dotnet user-secrets set "Agent:Mode" "LLM"  # or "Agent-LLM"

# For Mode 3 (Azure AI Foundry Agent) - instead of above
# dotnet user-secrets set "ConnectionStrings:aifoundry" "Endpoint=https://YOUR_PROJECT.api.azureml.ms/;Key=YOUR_KEY;"
# dotnet user-secrets set "Agent:Mode" "Agent-AIFoundry"
# dotnet user-secrets set "Agent:AgentId" "YOUR_AGENT_ID"

# 4. Run with Aspire
dotnet run
# OR press Ctrl+Shift+B in VS Code

# 5. Open in browser
# Aspire Dashboard: https://localhost:15216
# Blazor App: https://localhost:5001
```

**Replace:**

- `YOUR_RESOURCE`: Your Azure OpenAI resource name
- `YOUR_OPENAI_KEY`: Your Azure OpenAI API key
- `YOUR_SPEECH_KEY`: Your Azure Speech Service API key
- `gpt-4o-mini`: Your deployed model name

### Access Points

Once running, you'll have two URLs:

1. **Aspire Dashboard**: <https://localhost:15216>
   - Monitor all services in real-time
   - View logs, metrics, and traces
   - Check resource health and dependencies
   - Distributed tracing visualization

2. **Blazor Application**: <https://localhost:5001>
   - Main avatar chat interface
   - Configuration management
   - Interactive demo

## 📚 Documentation

### Getting Started

- **[Quick Start Guide](docs/QUICKSTART.md)** - Get running in 5 minutes
- **[Configuration Guide](#-configuration)** - Detailed setup options

### Architecture & Design  

- **[Architecture Overview](docs/ARCHITECTURE.md)** - System design and components
- **[Technology Stack](#-technology-stack)** - Libraries and frameworks used

### Deployment

- **[Deployment Guide](docs/DEPLOYMENT.md)** - Production deployment options
- **[Azure Provisioning](#-deployment)** - Automatic resource creation with `azd up`

### Migration & History

- **[Aspire Migration](docs/migration/)** - Migration from appsettings.json to Aspire
  - [Migration Summary](docs/migration/MIGRATION-SUMMARY.md) - Overview and key changes
  - [Implementation Details](docs/migration/IMPLEMENTATION-COMPLETE.md) - Technical implementation
  - [Final Summary](docs/migration/FINAL-SUMMARY.md) - Results and verification

## ⚙️ Configuration

### Aspire Connection Strings (Recommended)

With Aspire, all configuration is managed by the AppHost project:

```bash
cd AzureAIAvatarBlazor.AppHost

# Azure OpenAI (connection string format)
dotnet user-secrets set "ConnectionStrings:openai" \
  "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_KEY;"

# Azure Speech Service (connection string format)
dotnet user-secrets set "ConnectionStrings:speech" \
  "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"

# Application defaults
dotnet user-secrets set "Avatar:Character" "lisa"
dotnet user-secrets set "Avatar:Style" "casual-sitting"
dotnet user-secrets set "OpenAI:DeploymentName" "gpt-4o-mini"
dotnet user-secrets set "SystemPrompt" "You are a helpful AI assistant."
```

### Environment Variables (CI/CD)

For automated deployments and CI/CD pipelines:

**Windows PowerShell:**

```powershell
$env:ConnectionStrings__openai = "Endpoint=https://...;Key=...;"
$env:ConnectionStrings__speech = "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=...;"
$env:Avatar__Character = "lisa"
$env:OpenAI__DeploymentName = "gpt-4o-mini"
```

**Linux/macOS:**

```bash
export ConnectionStrings__openai="Endpoint=https://...;Key=...;"
export ConnectionStrings__speech="Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=...;"
export Avatar__Character="lisa"
export OpenAI__DeploymentName="gpt-4o-mini"
```

**Note:** Use double underscores (`__`) for nested configuration keys.

### Legacy Environment Variables (Backward Compatible)

The application also supports legacy variable names from the JavaScript version:

```bash
AZURE_SPEECH_REGION=westus2
AZURE_SPEECH_API_KEY=your_key
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your_key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
AVATAR_CHARACTER=lisa
TTS_VOICE=en-US-AvaMultilingualNeural
```

## 🏗️ Technology Stack

### Framework & Runtime

- **.NET 9** - Latest .NET framework with performance improvements
- **Blazor Server** - Server-side rendering with SignalR real-time communication
- **.NET Aspire 9.5** - Cloud-native orchestration and observability

### Azure SDK Integration

- **Aspire.Azure.AI.OpenAI (9.5.2-preview)** - Managed OpenAI client with DI
- **Microsoft.CognitiveServices.Speech (1.46.0)** - Speech SDK for STT/TTS/Avatar
- **Azure.AI.Projects (1.0.0-beta.2)** - Azure AI Foundry project client
- **Microsoft.Agents.AI (1.0.0-preview)** - Microsoft Agent Framework abstractions
- **Azure.AI.Agents.Persistent (1.2.0-beta.7)** - Agent Framework core library
- **Azure.Search.Documents (11.7.0)** - Optional search integration

### Aspire Components

- **Aspire.Hosting.AppHost** - Orchestration host
- **Aspire.Hosting.Azure.CognitiveServices** - Azure AI resource definitions
- **Aspire.Hosting.Azure.Search** - Search resource definitions
- **ServiceDefaults** - Shared telemetry and resilience patterns

### UI & Frontend

- **Bootstrap 5** - Responsive UI framework
- **Bootstrap Icons** - Icon library
- **JavaScript Interop** - Browser WebRTC integration

## 💬 Using the Application

### Home Page

- Welcome screen with feature overview
- Quick navigation to Chat or Configure

### Configuration Page (`/config`)

1. **Select Mode** - Choose between LLM, Agent-LLM, or Agent-AIFoundry
2. **Verify Credentials** - Check Azure service settings (varies by mode)
3. **Customize Avatar** - Choose character and style
4. **Adjust Settings** - Audio gain, subtitles, reconnect options
5. **Test Connections** - Validate Speech and OpenAI/Agent connectivity
6. **Save** - Persist to browser localStorage

**Mode-Specific Configuration:**

- **LLM**: Azure OpenAI endpoint, API key, deployment name
- **Agent-LLM**: Same as LLM mode + Agent Framework settings
- **Agent-AIFoundry**: AI Foundry project connection, agent ID, authentication

### Chat Page (`/chat`)

1. **Open Avatar Session** - Establish WebRTC connection
2. **Type Messages** - Enable "Type Message" and send text
3. **View Conversation** - Chat history in right panel
4. **Control Avatar** - Stop speaking, clear history, close session

## 🚢 Deployment

### Local Development

**Option 1: VS Code (Easiest)**

```bash
# Press Ctrl+Shift+B (default task)
# OR
# Ctrl+Shift+P → "Tasks: Run Task" → "Aspire: Run"
```

**Option 2: Command Line**

```bash
cd AzureAIAvatarBlazor.AppHost
dotnet run
```

**Option 3: Visual Studio 2022**

1. Open `AzureAIAvatarBlazor.slnx`
2. Set `AzureAIAvatarBlazor.AppHost` as startup project
3. Press **F5** to debug

### Azure Deployment (Production)

**One-Command Deployment with Azure Developer CLI:**

```bash
# Install Azure Developer CLI
winget install microsoft.azd  # Windows
brew install azd              # macOS
curl -fsSL https://aka.ms/install-azd.sh | bash  # Linux

# Initialize (one-time)
cd AzureAIAvatarBlazor.AppHost
azd init

# Deploy to Azure
azd up
```

**What `azd up` does:**

1. ✅ Creates Azure OpenAI resource
2. ✅ Deploys GPT-4o-mini model
3. ✅ Creates Azure Speech Service  
4. ✅ Creates Container Apps environment
5. ✅ Builds and deploys Blazor app as container
6. ✅ Configures managed identities (no keys needed!)
7. ✅ Sets up monitoring and logging

**Customize deployment:**

```bash
# Set custom region
azd env set AZURE_LOCATION westus2

# Set resource group prefix
azd env set AZURE_RESOURCE_GROUP_PREFIX rg-avatar

# Deploy
azd up
```

See [Deployment Guide](docs/DEPLOYMENT.md) for detailed options including Azure App Service, Container Apps, and Kubernetes.

## 🐛 Troubleshooting

### "Azure Speech credentials not configured"

**Solution**: Configure AppHost secrets:

```bash
cd AzureAIAvatarBlazor.AppHost
dotnet user-secrets set "ConnectionStrings:speech" \
  "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"
```

### "Failed to get avatar token"

**Causes:**

- Wrong Speech Service region
- Invalid API key
- Avatar feature not enabled
- Network connectivity issues

**Solution:**

1. Verify credentials in Azure Portal
2. Check region matches your resource
3. Test connection in Config page

### Avatar video won't load

**Causes:**

- Browser doesn't support WebRTC
- Firewall blocking WebRTC
- Certificate issues (self-signed HTTPS)

**Solutions:**

- Use Chrome or Edge (best WebRTC support)
- Check browser console (F12) for errors
- Try in incognito/private mode
- Disable browser extensions temporarily

### Chat not responding

**Causes:**

- Invalid OpenAI credentials
- Model not deployed
- Rate limits exceeded

**Solutions:**

1. Verify OpenAI endpoint and key
2. Check model deployment in Azure Portal
3. Test connection in Config page
4. Wait if rate limited

### Port already in use

**Error**: `Failed to bind to address http://127.0.0.1:5000`

**Solution:**

```bash
# Use different port
dotnet run --urls "http://localhost:5100;https://localhost:5101"
```

### Build errors

**Solutions:**

```bash
# Clear and restore
dotnet clean
dotnet restore
dotnet build

# Clear NuGet cache if needed
dotnet nuget locals all --clear
```

## 🎨 Customization

### Modify System Prompt

In AppHost user secrets:

```bash
dotnet user-secrets set "SystemPrompt" "You are a technical expert specializing in software development."
```

### Change Avatar

In AppHost user secrets:

```bash
dotnet user-secrets set "Avatar:Character" "harry"
dotnet user-secrets set "Avatar:Style" "business"
```

Available characters: `lisa`, `harry`, `jeff`, `lori`, `carla`

### Extend Services

Add new services in `AzureAIAvatarBlazor/Services/`:

```csharp
public interface IMyService
{
    Task<string> DoSomethingAsync();
}

public class MyService : IMyService
{
    public Task<string> DoSomethingAsync() => Task.FromResult("Done!");
}
```

Register in `Program.cs`:

```csharp
builder.Services.AddScoped<IMyService, MyService>();
```

### Add Aspire Resources

In `AppHost.cs`:

```csharp
var redis = builder.AddRedis("redis");
var postgres = builder.AddPostgres("postgres");

builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WithReference(redis)
    .WithReference(postgres);
```

## 🔒 Security

### Best Practices

- ✅ Use User Secrets for local development
- ✅ Use Managed Identities in production (no keys!)
- ✅ Use Azure Key Vault for secret storage
- ✅ Never commit secrets to source control
- ✅ Rotate keys regularly
- ✅ Use HTTPS in production

### What's Protected

- AppHost user secrets stored outside repository
- `.gitignore` excludes all credential files
- Aspire manages Azure credentials automatically
- Managed Identity eliminates key management in production

## 📚 Additional Resources

### Official Documentation

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Speech Service](https://docs.microsoft.com/azure/cognitive-services/speech-service/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/)
- [Azure AI Foundry Agent Types](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/azure-ai-foundry-agent?pivots=programming-language-csharp)
- [Azure AI Foundry Portal](https://ai.azure.com)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

### Code Samples

- [Aspire Samples](https://github.com/dotnet/aspire-samples)
- [Azure Speech SDK Samples](https://github.com/Azure-Samples/cognitive-services-speech-sdk)
- [Azure OpenAI .NET Samples](https://github.com/Azure-Samples/openai-dotnet-samples)

### Related

- [JavaScript Implementation](../python/README.md) - Lightweight web version
- [Avatar SDK Documentation](https://learn.microsoft.com/azure/ai-services/speech-service/how-to-use-avatar)

## 🤝 Contributing

See the main repository [CONTRIBUTING](../CONTRIBUTING.md) guide for details.

## 📄 License

MIT License - see [LICENSE](../LICENSE) file for details.

## 👥 Credits

- **Original Idea**: Pablo Piovano ([LinkedIn](https://www.linkedin.com/in/ppiova/))
- **Implementation**: Bruno Capuano ([GitHub](https://github.com/elbruno)) ([LinkedIn](https://www.linkedin.com/in/brunocapuano/))
- **Powered By**: Microsoft Azure AI Services & .NET Aspire

---

**Need help?** Check the [Quick Start Guide](docs/QUICKSTART.md) or [open an issue](https://github.com/elbruno/customavatarlabs/issues)
