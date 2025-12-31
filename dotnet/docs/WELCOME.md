# Welcome to the Azure AI Avatar Agent

## What is This Project?

The Azure AI Avatar Agent is a cutting-edge conversational AI application that brings together voice, intelligence, and visual presence to create truly immersive interactions. Imagine chatting with an AI that doesn't just respond with text, but speaks to you with a natural voice while displaying synchronized facial movements and lip movements through a talking avatar.

This project demonstrates how to build modern, cloud-native AI applications using Azure AI Services and .NET technologies.

## What Can It Do?

The AI Avatar Agent provides a complete conversational experience:

- **👂 Listen to You**: Converts your spoken words into text using Azure Speech-to-Text
- **🤖 Think Intelligently**: Generates contextual responses using Azure OpenAI's GPT models
- **🗣️ Speak Back**: Synthesizes natural-sounding speech with Azure Text-to-Speech
- **👤 Show Emotions**: Displays a realistic avatar with synchronized lip movements as it speaks
- **💬 Remember Context**: Maintains conversation history for coherent dialogues
- **🌍 Support Multiple Languages**: Recognizes and responds in 40+ languages

## The .NET Implementation

This repository includes **two complete implementations** of the same application. This guide focuses on the **.NET 10 Blazor implementation** with .NET Aspire orchestration, which is designed for enterprise and production scenarios.

### Why Choose the .NET Implementation?

The .NET version is ideal if you need:

✅ **Enterprise-grade reliability** with built-in resilience patterns  
✅ **Production observability** with OpenTelemetry for logs, metrics, and traces  
✅ **One-command deployment** to Azure with automatic resource provisioning  
✅ **Secure secret management** with managed identities (no API keys in production!)  
✅ **Developer productivity** with real-time monitoring via Aspire Dashboard  
✅ **Type safety and performance** that comes with compiled .NET code

## 🤖 Three Operational Modes

The .NET application supports **three different modes** for AI conversations, each designed for specific use cases:

### Mode 1: Chat with LLM (Default)

**Direct integration with Azure OpenAI**

- Uses standard Azure OpenAI chat completion API
- Simple request/response pattern with streaming support
- Best for: Quick demos, simple conversations, direct LLM access with agent capabilities
- Configuration: Model/deployment name only (uses Microsoft Foundry ChatClient)

### Mode 2: Agent-LLM

**Microsoft Agent Framework with ChatClient from Microsoft Foundry**

- Uses [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/) with ChatClient from Microsoft Foundry
- Enables agent-based workflows and orchestration patterns
- Supports tool calling, memory, and advanced agent behaviors
- Best for: Complex workflows, multi-step reasoning, tool integration without managing separate agents
- Configuration: Only requires model/deployment name (e.g., gpt-5.1-chat) - endpoint and credentials are provided by Microsoft Foundry project

### Mode 3: Chat with Azure AI Foundry Agent

**Pre-built agents from Azure AI Foundry**

- Connects to agents already created in [Azure AI Foundry](https://ai.azure.com)
- Uses Microsoft Agent Framework with pre-configured agent endpoints
- Supports grounding, RAG (Retrieval Augmented Generation), and custom tools
- Best for: Production scenarios, enterprise agents, grounded responses
- Configuration: Requires AI Foundry project ID, agent ID, and managed identity/key
- Learn more: [Azure AI Foundry Agent Types](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/azure-ai-foundry-agent?pivots=programming-language-csharp)

> **💡 Tip**: Switch between modes in the Configuration page (`/config`) under "Azure OpenAI / Agent Configuration" → "Mode" dropdown

## How Does It Work?

### The Big Picture

Here's what happens when you have a conversation with the AI Avatar:

```
┌─────────────────────────────────────────────────────────────────┐
│                         YOUR BROWSER                             │
│                                                                  │
│  ┌──────────────┐         ┌──────────────┐                     │
│  │   Chat UI    │◄───────►│ Avatar Video │                     │
│  │  (Blazor)    │         │   WebRTC     │                     │
│  └──────┬───────┘         └──────▲───────┘                     │
│         │                        │                              │
└─────────┼────────────────────────┼──────────────────────────────┘
          │                        │
          │ SignalR                │ WebRTC Stream
          │                        │
┌─────────▼────────────────────────┼──────────────────────────────┐
│                 .NET 10 BLAZOR SERVER                            │
│                                  │                               │
│  ┌───────────────────────────────┼─────────────────────────┐   │
│  │           APPLICATION SERVICES│                         │   │
│  │                              │                         │   │
│  │  ┌────────────┐  ┌──────────▼──────┐  ┌────────────┐  │   │
│  │  │  OpenAI    │  │     Speech      │  │   Config   │  │   │
│  │  │  Service   │  │     Service     │  │   Service  │  │   │
│  │  └─────┬──────┘  └─────────┬───────┘  └────────────┘  │   │
│  └────────┼───────────────────┼──────────────────────────┘   │
│           │                   │                               │
└───────────┼───────────────────┼───────────────────────────────┘
            │                   │
    ┌───────┼───────────────────┼───────┐
    │       │                   │       │
┌───▼───────▼────┐   ┌──────────▼───────▼──┐
│  Azure OpenAI  │   │   Azure Speech      │
│                │   │     Service         │
│  • GPT Models  │   │   • STT/TTS         │
│  • Streaming   │   │   • Avatar Video    │
└────────────────┘   └─────────────────────┘
```

### Step-by-Step Flow

Let's walk through a typical conversation:

**1. You speak into your microphone**

- Your browser captures the audio
- Azure Speech Service converts it to text (Speech-to-Text)

**2. The text is sent to Azure OpenAI**

- Your message is added to the conversation history
- Azure OpenAI generates an intelligent response
- The response is streamed back in real-time

**3. The AI response is converted to speech**

- Text flows to Azure Text-to-Speech
- A natural voice reads the response
- Simultaneously, the avatar synthesizer generates video

**4. You see and hear the avatar**

- WebRTC streams the avatar video to your browser
- Lip movements sync perfectly with the speech
- You see the avatar "speaking" to you in real-time

## The .NET Aspire Advantage

### What is .NET Aspire?

.NET Aspire is Microsoft's cloud-native application framework that makes it incredibly easy to build, deploy, and observe distributed applications. Think of it as the "control center" for your application.

### Aspire Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    .NET ASPIRE APPHOST                           │
│                   (The Orchestrator)                             │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              RESOURCE DEFINITIONS                          │ │
│  │                                                            │ │
│  │  • Azure OpenAI with GPT-4o-mini deployment               │ │
│  │  • Azure Speech Service                                   │ │
│  │  • Azure Cognitive Search (optional)                      │ │
│  │  • Connection strings and environment variables           │ │
│  └────────────────────────────────────────────────────────────┘ │
│                              │                                  │
│                              │ Injects Configuration            │
└──────────────────────────────┼──────────────────────────────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
      ┌───────▼────────┐  ┌────▼────────────┐  ┌▼──────────────────┐
      │    Aspire      │  │    Blazor App   │  │  Azure Resources  │
      │   Dashboard    │  │                 │  │  (Production)     │
      │  localhost:    │  │  Services       │  │  • OpenAI         │
      │    15216       │  │  Components     │  │  • Speech         │
      │                │  │  Pages          │  │  • Search         │
      │  • Logs        │  └─────────────────┘  └───────────────────┘
      │  • Metrics     │
      │  • Traces      │
      │  • Health      │
      └────────────────┘
```

### Key Benefits

**1. No Configuration Files**

- Traditional apps: Manage `appsettings.json`, `appsettings.Development.json`, etc.
- Aspire apps: All configuration is centralized in the AppHost
- Result: No more config file confusion!

**2. Automatic Resource Provisioning**

- Run `azd up` and Aspire:
  - Creates Azure OpenAI resource
  - Deploys the GPT model
  - Creates Speech Service
  - Configures managed identities
  - Deploys your application
- Result: Production deployment in minutes, not hours!

**3. Built-in Observability**

- OpenTelemetry is pre-configured
- Real-time dashboard at <https://localhost:15216> shows:
  - All logs from your application
  - Performance metrics and traces
  - Health checks for each service
  - Dependencies and service maps
- Result: Debug issues faster with complete visibility!

**4. Service Discovery**

- Services automatically find each other
- No hardcoded URLs or endpoints
- Works locally and in the cloud
- Result: Simplified development and deployment!

## Technical Architecture

### Application Layers

The .NET implementation follows a clean, layered architecture:

```
┌─────────────────────────────────────────────────────────────────┐
│                     PRESENTATION LAYER                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Home.razor   │  │ Chat.razor   │  │ Config.razor │          │
│  │              │  │              │  │              │          │
│  │ Landing page │  │ Chat UI      │  │ Settings UI  │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                       SERVICE LAYER                              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  IAzureOpenAIService                                     │   │
│  │  • GetChatCompletionStreamAsync()                       │   │
│  │  • Manages Azure OpenAI interactions                    │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  IAzureSpeechService                                     │   │
│  │  • ValidateConnectionAsync()                            │   │
│  │  • Provides Speech credentials                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  IConfigurationService                                   │   │
│  │  • GetConfiguration()                                   │   │
│  │  • Manages app settings                                 │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                        MODELS LAYER                              │
│  • AvatarConfiguration                                           │
│  • ChatMessage                                                   │
│  • PromptProfile                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### JavaScript Interop Layer

Since the Azure Speech SDK's avatar feature requires browser APIs (WebRTC), we use JavaScript interop:

```
┌─────────────────────────────────────────────────────────────────┐
│                 C# BLAZOR COMPONENTS                             │
│                                                                  │
│  Chat.razor: await JS.InvokeAsync("startAvatarSession", ...)   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               │ IJSRuntime
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│               JAVASCRIPT INTEROP LAYER                           │
│                                                                  │
│  avatar-interop.js:                                             │
│  • startAvatarSession()                                         │
│  • speakText()                                                  │
│  • stopSpeaking()                                               │
│  • closeAvatarSession()                                         │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│              AZURE SPEECH SDK (BROWSER)                          │
│                                                                  │
│  • AvatarSynthesizer                                            │
│  • RTCPeerConnection (WebRTC)                                   │
│  • MediaStream                                                  │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BROWSER VIDEO ELEMENT                         │
└─────────────────────────────────────────────────────────────────┘
```

### Azure Services Integration

The application integrates with Azure services differently depending on the operational mode:

#### 1. Azure OpenAI Service (Modes 1 & 2)

**Purpose**: Provides intelligent conversation capabilities

**What it does**:

- Receives user messages with conversation history
- Generates contextual, intelligent responses
- Streams responses in real-time for better UX

**How we use it**:

```csharp
// Mode 1: Direct LLM
// Aspire injects AzureOpenAIClient automatically
var client = serviceProvider.GetRequiredService<AzureOpenAIClient>();
var chatClient = client.GetChatClient("gpt-4o-mini");

// Stream responses
await foreach (var update in chatClient.CompleteChatStreamingAsync(messages))
{
    // Update UI in real-time
}

// Mode 2: Agent-LLM
// Uses Microsoft Agent Framework with Azure OpenAI
var agent = await AIAgent.CreateAsync(chatClient, options);
var response = await agent.InvokeAsync(userMessage);
```

#### 1b. Azure AI Foundry (Mode 3)

**Purpose**: Connects to pre-built enterprise agents

**What it does**:

- Uses agents created in Azure AI Foundry portal
- Supports RAG (Retrieval Augmented Generation)
- Includes grounding with custom data sources
- Enables tool calling and function execution

**How we use it**:

```csharp
// Mode 3: Agent-AIFoundry
var projectClient = new AIProjectClient(connectionString, credential);
var agent = await projectClient.GetAgentAsync(agentId);
var response = await agent.InvokeAsync(userMessage);
```

#### 2. Azure Speech Service

**Purpose**: Handles all voice and avatar functionality

**What it does**:

- **Speech-to-Text (STT)**: Converts your voice to text
- **Text-to-Speech (TTS)**: Converts AI responses to natural speech
- **Avatar Synthesis**: Generates video with lip-sync animation

**How we use it**:

```javascript
// JavaScript (browser-side)
const avatarSynthesizer = new SpeechSDK.AvatarSynthesizer(
    speechConfig, 
    avatarConfig
);

await avatarSynthesizer.speakTextAsync(aiResponse);
```

#### 3. Azure Cognitive Search (Optional)

**Purpose**: Enables "On Your Data" scenarios

**What it does**:

- Connects to your custom knowledge base
- Enhances AI responses with your organization's data
- Provides citations and sources

## Configuration Management

### Development Setup

For local development, all configuration is managed through **User Secrets**:

```bash
cd AzureAIAvatarBlazor.AppHost

# Required for all modes - Azure Speech Service
dotnet user-secrets set "ConnectionStrings:speech" \
  "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"
dotnet user-secrets set "Avatar:Character" "lisa"

# For Agent-LLM mode (uses Microsoft Foundry ChatClient)
dotnet user-secrets set "AzureOpenAI:Mode" "Agent-LLM"
dotnet user-secrets set "AzureOpenAI:AgentLLM:DeploymentName" "gpt-5.1-chat"
dotnet user-secrets set "AzureOpenAI:AgentLLM:SystemPrompt" "You are a helpful AI assistant."

# For Agent-MicrosoftFoundry mode (requires Microsoft Foundry project endpoint via Aspire)
# dotnet user-secrets set "AzureOpenAI:Mode" "Agent-MicrosoftFoundry"
# dotnet user-secrets set "AzureOpenAI:AgentMicrosoftFoundry:MicrosoftFoundryAgentName" "YOUR_AGENT_NAME"

# For Agent-AIFoundry mode (not yet implemented)
# dotnet user-secrets set "AzureOpenAI:Mode" "Agent-AIFoundry"
# dotnet user-secrets set "AzureOpenAI:AgentAIFoundry:AIFoundryEndpoint" "https://YOUR_PROJECT.api.azureml.ms/"
# dotnet user-secrets set "AzureOpenAI:AgentAIFoundry:AgentId" "YOUR_AGENT_ID"
```

**Why User Secrets?**

- ✅ Secrets are stored **outside** your project directory
- ✅ Never accidentally committed to Git
- ✅ Each developer has their own credentials
- ✅ No risk of exposing keys in screenshots or demos

### Production Deployment

In production, Aspire uses **Managed Identities**:

```
┌─────────────────────────────────────────────────────────────────┐
│                    YOUR BLAZOR APP                               │
│                (Azure Container App)                             │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │         System-Assigned Managed Identity                  │  │
│  │  (Automatic authentication - no keys!)                   │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────────────────────────┬───────────────────────────────────────┘
                           │
                           │ Authenticated connection
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                     AZURE SERVICES                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │    OpenAI    │  │    Speech    │  │    Search    │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
```

**Benefits**:

- 🔒 **No API keys stored anywhere** - identities authenticate automatically
- 🔄 **Auto-rotation** - no manual credential management
- 🎯 **Least privilege** - grant only the access needed
- 📊 **Full audit trail** - all access is logged

## Getting Started

### Prerequisites

Before you begin, ensure you have:

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** installed
2. **.NET Aspire workload**: Run `dotnet workload install aspire`
3. **Azure Subscription** - [Create a free account](https://azure.microsoft.com/free/)
4. **Azure OpenAI access** - [Request access](https://aka.ms/oai/access)

### 5-Minute Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/elbruno/customavatarlabs.git
cd customavatarlabs/dotnet

# 2. Install Aspire workload (if not done)
dotnet workload install aspire

# 3. Configure your Azure credentials
cd AzureAIAvatarBlazor.AppHost

# Required for all modes
dotnet user-secrets set "ConnectionStrings:speech" \
  "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"
dotnet user-secrets set "Avatar:Character" "lisa"

# For Mode 1 (LLM) - Default mode
dotnet user-secrets set "ConnectionStrings:speech" \
  "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"

# For Agent-LLM mode (uses Microsoft Foundry ChatClient)
dotnet user-secrets set "AzureOpenAI:Mode" "Agent-LLM"
dotnet user-secrets set "AzureOpenAI:AgentLLM:DeploymentName" "gpt-5.1-chat"

# For Agent-MicrosoftFoundry mode (requires Microsoft Foundry project via Aspire)
# dotnet user-secrets set "AzureOpenAI:Mode" "Agent-MicrosoftFoundry"
# dotnet user-secrets set "AzureOpenAI:AgentMicrosoftFoundry:MicrosoftFoundryAgentName" "YOUR_AGENT"

# For Agent-AIFoundry mode (not yet implemented)
# dotnet user-secrets set "AzureOpenAI:Mode" "Agent-AIFoundry"
# dotnet user-secrets set "AzureOpenAI:AgentAIFoundry:AIFoundryEndpoint" "https://..."
# dotnet user-secrets set "AzureOpenAI:AgentAIFoundry:AgentId" "YOUR_AGENT_ID"

# 4. Run the application
dotnet run

# 5. Open your browser
# - Aspire Dashboard: https://localhost:15216
# - Blazor App: https://localhost:5001
```

### What You'll See

**Aspire Dashboard** (<https://localhost:15216>):

- Real-time logs from your application
- Performance metrics and traces
- Health status of all services
- Distributed tracing visualization

**Blazor Application** (<https://localhost:5001>):

- Home page with project overview
- Configuration page to manage settings
- Chat page with interactive avatar

## Project Structure

```
dotnet/
├── AzureAIAvatarBlazor.AppHost/        # Aspire orchestration host
│   ├── AppHost.cs                      # Resource definitions
│   └── Program.cs                      # Entry point
│
├── AzureAIAvatarBlazor.ServiceDefaults/ # Shared configuration
│   └── Extensions.cs                    # Telemetry setup
│
└── AzureAIAvatarBlazor/                # Main Blazor application
    ├── Components/
    │   ├── Pages/
    │   │   ├── Home.razor              # Landing page
    │   │   ├── Chat.razor              # Avatar chat interface
    │   │   └── Config.razor            # Settings management
    │   └── Layout/
    │       ├── MainLayout.razor        # App shell
    │       └── NavMenu.razor           # Navigation
    │
    ├── Services/                       # Business logic
    │   ├── AzureOpenAIService.cs       # OpenAI integration
    │   ├── AzureSpeechService.cs       # Speech Service integration
    │   └── ConfigurationService.cs     # App settings
    │
    ├── Models/                         # Data models
    │   ├── AvatarConfiguration.cs
    │   └── ChatMessage.cs
    │
    ├── wwwroot/
    │   └── js/
    │       └── avatar-interop.js       # JavaScript interop
    │
    └── Program.cs                      # Application startup
```

## Key Features

### 1. Real-Time Streaming

Responses from Azure OpenAI are streamed in real-time, providing a more interactive experience:

```csharp
await foreach (var update in chatClient.CompleteChatStreamingAsync(messages))
{
    if (update.ContentUpdate.Count > 0)
    {
        // Update UI immediately with each chunk
        responseBuilder.Append(update.ContentUpdate[0].Text);
        await InvokeAsync(StateHasChanged);
    }
}
```

### 2. Multi-Language Support

The avatar automatically detects and responds in your language:

- Supports 40+ languages for Speech-to-Text
- 400+ neural voices for Text-to-Speech
- Automatic language detection
- Seamless language switching

### 3. Custom Avatars

Choose from built-in avatars or bring your own:

- **Built-in**: Lisa, Harry, Jeff, Lori, Carla
- **Custom**: Upload your own avatar video
- **Styles**: Casual, Business, Formal
- **Backgrounds**: Transparent, Office, Studio

### 4. Resilience Patterns

Built-in retry and circuit breaker patterns via Aspire:

```csharp
// Automatic retries with exponential backoff
// Automatic circuit breaker to prevent cascading failures
// All configured via ServiceDefaults
```

### 5. Health Checks

Monitor the health of your application:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("speech-service", () => 
        speechService.ValidateConnectionAsync())
    .AddCheck("openai-service", () => 
        openAIService.ValidateConnectionAsync());
```

## Deployment to Azure

### One-Command Deployment

Aspire makes Azure deployment incredibly simple:

```bash
# Install Azure Developer CLI
winget install microsoft.azd  # Windows
brew install azd              # macOS

# Deploy everything to Azure
cd AzureAIAvatarBlazor.AppHost
azd up
```

### What Gets Deployed

Running `azd up` automatically:

1. ✅ **Creates Azure Resource Group**
2. ✅ **Provisions Azure OpenAI** with GPT-4o-mini deployment
3. ✅ **Provisions Azure Speech Service**
4. ✅ **Creates Container Apps Environment**
5. ✅ **Builds and containerizes your app**
6. ✅ **Deploys to Azure Container Apps**
7. ✅ **Configures managed identities**
8. ✅ **Sets up monitoring and logging**
9. ✅ **Returns your application URL**

**Total time**: 5-10 minutes for complete production deployment!

## Observability and Monitoring

### Aspire Dashboard

The Aspire Dashboard provides comprehensive observability:

**Logs Tab**:

- Real-time log streaming
- Filter by severity and source
- Search across all logs
- Contextual information

**Metrics Tab**:

- CPU and memory usage
- Request rates and latencies
- Custom application metrics
- Historical charts

**Traces Tab**:

- Distributed tracing
- Request flow visualization
- Performance bottleneck identification
- Dependency mapping

**Resources Tab**:

- All services and their status
- Connection strings (redacted)
- Health check results
- Environment variables

### OpenTelemetry Integration

The application automatically exports telemetry to:

- **Development**: Aspire Dashboard
- **Production**: Azure Application Insights, Prometheus, Grafana, etc.

No code changes needed - just configure the endpoint!

## Security Best Practices

### What This Project Does Right

✅ **No secrets in code**: All credentials via User Secrets or environment variables  
✅ **No secrets in Git**: `.gitignore` protects sensitive files  
✅ **Managed Identities**: Production uses identity-based auth (no keys!)  
✅ **Least privilege**: Services only have required permissions  
✅ **HTTPS by default**: All communication encrypted  
✅ **Input validation**: User input sanitized and validated

### What You Should Do

✅ **Rotate keys regularly** if using API keys  
✅ **Monitor access logs** for suspicious activity  
✅ **Use Azure Key Vault** for centralized secret management  
✅ **Enable Azure AD authentication** for user access control  
✅ **Review permissions** periodically

## Common Questions

### Q: How is this different from the JavaScript version?

**JavaScript version** (`python/` folder):

- ✅ Quick to start and deploy
- ✅ No backend server needed
- ✅ Perfect for demos and prototypes
- ❌ Limited observability
- ❌ Manual Azure resource setup
- ❌ Client-side secret management

**.NET version** (`dotnet/` folder):

- ✅ Enterprise-grade architecture
- ✅ Built-in observability and telemetry
- ✅ One-command Azure deployment
- ✅ Managed identity support
- ✅ Type safety and performance
- ❌ Requires .NET knowledge
- ❌ Slightly more complex setup

### Q: Do I need to know JavaScript for the .NET version?

Minimal JavaScript is needed only for the avatar WebRTC connection. The JavaScript interop code (`avatar-interop.js`) is pre-written and ready to use. You only need to know C# to customize the application.

### Q: Can I use my own avatar?

Yes! You can use custom avatar videos. Configure the `Avatar:Character` setting to your custom avatar identifier and ensure `Avatar:IsCustomAvatar` is set to `true`.

### Q: What about costs?

Azure costs depend on usage:

- **Azure OpenAI**: Pay per token (input and output)
- **Azure Speech Service**: Pay per character synthesized and hours of transcription
- **Azure Container Apps**: Pay for compute time (includes free tier)

For development and demos, costs are typically $5-20/month. Use Azure Cost Management to monitor spending.

### Q: Can I run this completely offline?

No, the application requires internet connectivity to access Azure services. However, you can run Azure OpenAI and Speech Service endpoints in your own Azure private network for enhanced security.

## Next Steps

### Learn More

1. **[Quick Start Guide](dotnet/docs/QUICKSTART.md)** - Get running in 5 minutes
2. **[Architecture Deep Dive](dotnet/docs/ARCHITECTURE.md)** - Technical details
3. **[Deployment Guide](dotnet/docs/DEPLOYMENT.md)** - Production deployment options
4. **[Full README](dotnet/README.md)** - Complete documentation

### Customize Your Avatar

- Explore different avatar characters and styles
- Adjust system prompts for different personalities
- Configure audio gain for optimal volume
- Enable subtitles for accessibility

### Contribute

We welcome contributions! Check out:

- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Contribution guidelines
- **[GitHub Issues](https://github.com/elbruno/customavatarlabs/issues)** - Bug reports and feature requests
- **[GitHub Discussions](https://github.com/elbruno/customavatarlabs/discussions)** - Questions and ideas

## Credits and Acknowledgments

This project is built with:

- **[.NET 10](https://dotnet.microsoft.com/)** - Application framework
- **[.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)** - Cloud-native orchestration
- **[Azure OpenAI](https://azure.microsoft.com/products/ai-services/openai-service)** - GPT models
- **[Azure Speech Service](https://azure.microsoft.com/products/ai-services/ai-speech)** - Voice and avatar
- **[Azure MCP Server](https://aka.ms/azmcp/ga)** - Model Context Protocol integration
- **[Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)** - Interactive web UI

**Created by**:

- **Pablo Piovano** ([LinkedIn](https://www.linkedin.com/in/ppiova/)) - Original concept
- **Bruno Capuano** ([GitHub](https://github.com/elbruno) | [LinkedIn](https://www.linkedin.com/in/brunocapuano/)) - Implementation

**License**: [MIT License](LICENSE)

---

**Ready to get started?** Follow the [Quick Start Guide](dotnet/docs/QUICKSTART.md) and have your AI avatar running in 5 minutes!

For the JavaScript/HTML version, see the [Python folder README](python/README.md).
