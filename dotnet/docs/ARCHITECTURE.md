# Architecture and Design Documents

## System Architecture

### High-Level Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                         User Browser                            │
├────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │   Chat UI    │  │  Config UI   │  │   Home UI    │         │
│  │  (Blazor)    │  │  (Blazor)    │  │  (Blazor)    │         │
│  └──────┬───────┘  └──────┬───────┘  └──────────────┘         │
│         │                  │                                     │
│  ┌──────▼──────────────────▼────────────────────────────┐      │
│  │           JavaScript Interop Layer                    │      │
│  │  • avatar-interop.js                                  │      │
│  │  • Azure Speech SDK (Browser Package)                 │      │
│  │  • WebRTC for video streaming                         │      │
│  └──────────────────────┬────────────────────────────────┘      │
└────────────────────────┼────────────────────────────────────────┘
                         │
                    SignalR/WebSocket
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                   Blazor Server (.NET 10)                         │
├──────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    Presentation Layer                    │    │
│  │  • Pages (Chat.razor, Config.razor, Home.razor)         │    │
│  │  • Layout Components (NavMenu, MainLayout)              │    │
│  └──────────────────────┬───────────────────────────────────┘    │
│                         │                                         │
│  ┌──────────────────────▼───────────────────────────────────┐    │
│  │                    Service Layer                         │    │
│  │  ┌───────────────┐  ┌────────────────┐  ┌────────────┐ │    │
│  │  │   OpenAI      │  │    Speech      │  │   Config   │ │    │
│  │  │   Service     │  │    Service     │  │   Service  │ │    │
│  │  └───────┬───────┘  └───────┬────────┘  └─────┬──────┘ │    │
│  └──────────┼──────────────────┼──────────────────┼────────┘    │
│             │                  │                  │              │
│  ┌──────────▼──────────────────▼──────────────────▼────────┐    │
│  │                     Models Layer                         │    │
│  │  • AvatarConfiguration                                   │    │
│  │  • ChatMessage                                           │    │
│  │  • PromptProfile                                         │    │
│  └──────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────┘
                         │
            ┌────────────┼────────────┐
            │            │            │
┌───────────▼──┐  ┌──────▼──────┐  ┌─▼──────────────┐
│   Azure      │  │   Azure     │  │  Azure         │
│   Speech     │  │   OpenAI    │  │  Cognitive     │
│   Service    │  │   Service   │  │  Search        │
│              │  │             │  │  (Optional)    │
│  • STT/TTS   │  │  • Chat API │  │  • Search API  │
│  • Avatar    │  │  • Streaming│  │  • On Your Data│
└──────────────┘  └─────────────┘  └────────────────┘
```

## Aspire Orchestration Layer

### Overview

.NET Aspire acts as the orchestration layer, managing:
- **Resource provisioning**: Automatic creation of Azure AI resources
- **Configuration injection**: Connection strings and environment variables
- **Service discovery**: Automatic endpoint resolution
- **Telemetry**: Unified logging, metrics, and tracing via OpenTelemetry

### Architecture Diagram (with Aspire)

```
┌─────────────────────────────────────────────────────────────────┐
│                    .NET Aspire AppHost                           │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Resource Definitions                                       │ │
│  │  • Azure OpenAI (with deployment)                          │ │
│  │  • Azure Speech Service                                    │ │
│  │  • Azure Cognitive Search (optional)                       │ │
│  └─────────────┬──────────────────────────────────────────────┘ │
│                │ Connection Strings + Env Vars                  │
└────────────────┼────────────────────────────────────────────────┘
                 │
        ┌────────┼────────┐
        ▼                 ▼
┌─────────────────┐  ┌─────────────────────────────────┐
│  Aspire         │  │   Azure Resources (Publish)     │
│  Dashboard      │  │   • OpenAI + Deployment         │
│  (Dev Only)     │  │   • Speech Service              │
│  localhost:15216│  │   • Cognitive Search            │
└─────────────────┘  └─────────────────────────────────┘
        │
        │ Telemetry (OTLP)
        ▼
┌─────────────────────────────────────────────────────────────────┐
│               AzureAIAvatarBlazor Application                    │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Aspire-Managed Clients                                    │ │
│  │  • AzureOpenAIClient (injected via DI)                    │ │
│  │  • Speech credentials (from ConnectionStrings)            │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Application Services                                      │ │
│  │  • AzureOpenAIService (uses injected client)              │ │
│  │  • AzureSpeechService (reads connection strings)          │ │
│  │  • ConfigurationService (env vars only)                   │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Configuration Flow

**Development Mode** (local):
1. Developer sets AppHost user secrets
2. AppHost reads secrets, creates connection strings
3. AppHost injects connection strings into Blazor app as environment variables
4. Blazor app services read from `IConfiguration` (backed by env vars)
5. Aspire client libraries auto-configure from connection strings

**Publish Mode** (production):
1. `azd up` or `dotnet publish` triggers Azure provisioning
2. Aspire creates Azure resources (OpenAI, Speech, Search)
3. Aspire deploys model to OpenAI resource
4. Aspire configures managed identities
5. Blazor app uses managed identity authentication (no keys needed!)

### Benefits

- ✅ **No secrets in code**: All credentials managed externally
- ✅ **Single source of truth**: AppHost is the configuration authority
- ✅ **Environment parity**: Dev and prod use same config model
- ✅ **Automatic provisioning**: No manual Azure Portal setup
- ✅ **Built-in telemetry**: OpenTelemetry out of the box
- ✅ **Service discovery**: Endpoints resolved automatically

## Component Details

### 1. Presentation Layer

#### Pages
- **Home.razor**: Landing page with feature overview
- **Chat.razor**: Interactive chat interface with avatar
- **Config.razor**: Configuration management UI

#### Layout Components
- **MainLayout.razor**: Application shell
- **NavMenu.razor**: Navigation sidebar

### 2. Service Layer

#### AzureOpenAIService
**Purpose**: Manage Azure OpenAI chat completions using Aspire-managed client

**Key Methods**:
- `GetChatCompletionStreamAsync()`: Stream chat responses

**Dependencies**:
- `AzureOpenAIClient` (injected by Aspire via DI)
- IConfiguration (for deployment name)

**Implementation**:
```csharp
public AzureOpenAIService(
    AzureOpenAIClient client, // Aspire-managed
    IConfiguration configuration,
    ILogger<AzureOpenAIService> logger)
{
    _client = client;
    // No manual credential management needed
}
```

**Flow**:
```
User Message → Service → Injected Client → Azure OpenAI API → Stream Response → UI
```

#### AzureSpeechService
**Purpose**: Provide Speech Service credentials parsed from Aspire connection strings

**Key Methods**:
- `ValidateConnectionAsync()`: Test credentials
- `GetRegion()`: Retrieve region
- `GetSubscriptionKey()`: Parse key from connection string

**Dependencies**:
- IConfiguration (reads `ConnectionStrings:speech`)

**Implementation**:
```csharp
public string GetSubscriptionKey()
{
    var connectionString = _configuration["ConnectionStrings:speech"];
    // Parse: "Endpoint=...;Key=abc123;"
    var keyMatch = Regex.Match(connectionString, @"Key=([^;]+)");
    return keyMatch.Success ? keyMatch.Groups[1].Value : string.Empty;
}
```

#### ConfigurationService
**Purpose**: Manage application configuration

**Key Methods**:
- `GetConfiguration()`: Load settings
- `SaveConfigurationAsync()`: Persist settings
- `GetPromptProfilesAsync()`: Load prompt profiles

**Dependencies**:
- IConfiguration
- IWebHostEnvironment

### 3. Models Layer

#### AvatarConfiguration
```csharp
public class AvatarConfiguration
{
    public AzureSpeechConfig AzureSpeech { get; set; }
    public AzureOpenAIConfig AzureOpenAI { get; set; }
    public SttTtsConfig SttTts { get; set; }
    public AvatarDisplayConfig Avatar { get; set; }
    public AzureCognitiveSearchConfig? AzureCognitiveSearch { get; set; }
}
```

#### ChatMessage
```csharp
public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## Data Flow Diagrams

### Chat Message Flow

```
┌─────────┐
│  User   │
└────┬────┘
     │ 1. Type message
     ▼
┌─────────────────┐
│  Chat.razor     │
└────┬────────────┘
     │ 2. SendMessage()
     ▼
┌─────────────────────┐
│ AzureOpenAIService  │
└────┬────────────────┘
     │ 3. HTTP POST with streaming
     ▼
┌─────────────────────┐
│ Azure OpenAI API    │
└────┬────────────────┘
     │ 4. Stream chunks
     ▼
┌─────────────────────┐
│ AzureOpenAIService  │
└────┬────────────────┘
     │ 5. Yield chunks
     ▼
┌─────────────────┐
│  Chat.razor     │
└────┬────────────┘
     │ 6. Update UI + Speak
     ▼
┌─────────────────────┐
│ avatar-interop.js   │
└────┬────────────────┘
     │ 7. speakText()
     ▼
┌─────────────────────┐
│ Avatar Synthesizer  │
└────┬────────────────┘
     │ 8. WebRTC stream
     ▼
┌─────────────────┐
│ Video Element   │
└─────────────────┘
```

### Avatar Session Initialization

```
┌─────────┐
│  User   │
└────┬────┘
     │ 1. Click "Open Avatar Session"
     ▼
┌─────────────────┐
│  Chat.razor     │
└────┬────────────┘
     │ 2. StartAvatarSession()
     ▼
┌─────────────────────┐
│ avatar-interop.js   │
└────┬────────────────┘
     │ 3. Request relay token
     ▼
┌─────────────────────────────┐
│ Azure Speech Token Endpoint │
└────┬────────────────────────┘
     │ 4. Return ICE server config
     ▼
┌─────────────────────┐
│ avatar-interop.js   │
└────┬────────────────┘
     │ 5. Create RTCPeerConnection
     ▼
┌─────────────────────┐
│ WebRTC Connection   │
└────┬────────────────┘
     │ 6. Initialize avatar synthesizer
     ▼
┌─────────────────────┐
│ Avatar Synthesizer  │
└────┬────────────────┘
     │ 7. Start avatar async
     ▼
┌─────────────────────┐
│ WebRTC Media Stream │
└────┬────────────────┘
     │ 8. ontrack event
     ▼
┌─────────────────┐
│ Video Element   │
└─────────────────┘
```

## Technology Stack Details

### Backend (.NET 10)
- **Runtime**: .NET 10.0
- **Framework**: ASP.NET Core 10.0
- **UI**: Blazor Server (Interactive Server Render Mode)
- **Communication**: SignalR over WebSocket

### NuGet Packages
- `Azure.AI.OpenAI` (2.1.0)
- `Microsoft.CognitiveServices.Speech` (1.41.1)
- `Azure.Search.Documents` (11.7.0)
- `Microsoft.Extensions.Configuration.UserSecrets` (9.0.10)

### Frontend
- **UI Framework**: Bootstrap 5.3.3
- **Icons**: Bootstrap Icons 1.11.3
- **JavaScript**: Azure Speech SDK (Browser Package)
- **WebRTC**: Native browser APIs

### Development Tools
- Visual Studio 2022 / VS Code / Rider
- .NET CLI
- Browser DevTools
