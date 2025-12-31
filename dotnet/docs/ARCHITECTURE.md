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
│  │  Resource Definitions (Environment-Specific)                │ │
│  │  • Application Insights (Dev: conn string / Prod: Azure)   │ │
│  │  • Microsoft Foundry Project (optional, connection string) │ │
│  │  • Azure Tenant ID (optional, connection string)           │ │
│  │  • Azure Cognitive Search (optional)                       │ │
│  └─────────────┬──────────────────────────────────────────────┘ │
│                │ Connection Strings + Env Vars                  │
└────────────────┼────────────────────────────────────────────────┘
                 │
        ┌────────┼────────┐
        ▼                 ▼
┌─────────────────┐  ┌─────────────────────────────────┐
│  Aspire         │  │   Azure Resources (Publish)     │
│  Dashboard      │  │   • Application Insights        │
│  (Dev Only)     │  │   • Microsoft Foundry Project   │
│  localhost:15216│  │   • Cognitive Search            │
└─────────────────┘  └─────────────────────────────────┘
        │
        │ Telemetry (OTLP + Azure Monitor)
        ▼
┌─────────────────────────────────────────────────────────────────┐
│               AzureAIAvatarBlazor Application                    │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Aspire-Managed Clients & Infrastructure                   │ │
│  │  • Application Insights (automatic via ServiceDefaults)   │ │
│  │  • MAFFoundry Library (optional, IChatClient)             │ │
│  │  • Speech credentials (from env vars/config)              │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Application Services                                      │ │
│  │  • AzureAIAgentService (uses MAF or direct OpenAI)        │ │
│  │  • ConfigurationService (env vars + user secrets)         │ │
│  │  • TelemetryService (custom metrics & tracing)            │ │
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

#### TelemetryService
**Purpose**: Track custom telemetry events and metrics

**Key Methods**:
- `TrackAvatarSessionStart()`: Record avatar session initiation
- `TrackAvatarSessionEnd()`: Record session completion with duration
- `TrackChatMessage()`: Log chat message events
- `TrackAIResponseTime()`: Measure AI response performance
- `StartActivity()`: Create custom distributed tracing spans
- `TrackConfigurationChange()`: Log configuration updates
- `TrackError()`: Log exceptions with context

**Custom Metrics**:
- `avatar.sessions.started`: Counter for avatar sessions
- `chat.messages.sent`: Counter for chat messages by role
- `ai.response.duration`: Histogram of AI response times
- `avatar.session.duration`: Histogram of session durations

**Custom Activity Sources**:
- `AzureAIAvatarBlazor`: Custom tracing for application operations

**Dependencies**:
- ILogger
- System.Diagnostics.ActivitySource (for distributed tracing)
- System.Diagnostics.Metrics.Meter (for custom metrics)

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

### 4. Infrastructure Layer

#### MAFFoundry Library (AzureAIAvatarBlazor.MAFFoundry)
**Purpose**: Manages Microsoft Foundry project integration

**Key Classes**:
- `MAFFoundryAgentProvider`: Core provider for Foundry agents and clients
- `MAFFoundryAgentExtensions`: DI registration extensions

**Key Methods**:
```csharp
// MAFFoundryAgentProvider
public IChatClient GetChatClient(string? deploymentName = null)
public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(string? deploymentName = null)
public AIAgent GetAIAgent(string agentName, List<AITool>? tools = null)
public AIAgent GetOrCreateAIAgent(string agentName, string model, string instructions, List<AITool>? tools = null)

// MAFFoundryAgentExtensions
public static WebApplicationBuilder AddMAFFoundryAgents(this WebApplicationBuilder builder)
```

**Configuration**:
- Connection string: `microsoftfoundryproject`
- Tenant ID: `tenantId` (optional)
- Automatic fallback if not configured

**What it provides**:
- `IChatClient`: Registered in DI for chat operations
- `IEmbeddingGenerator<string, Embedding<float>>`: Registered in DI for embeddings
- `MAFFoundryAgentProvider`: Registered as singleton for advanced scenarios

**Dependencies**:
- `Azure.AI.Projects` (AIProjectClient)
- `Azure.Identity` (DefaultAzureCredential)
- `Azure.AI.OpenAI` (AzureOpenAIClient)
- Microsoft.Agents.AI packages

**See**: [MAFFOUNDRY_LIBRARY.md](./MAFFOUNDRY_LIBRARY.md) for detailed documentation

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

## Telemetry and Observability

### Overview

The application implements comprehensive telemetry using **Azure Application Insights** and **OpenTelemetry** standards, providing end-to-end observability in both development and production environments.

### Telemetry Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                Application Code                               │
│  ┌────────────────┐  ┌──────────────────┐  ┌──────────────┐ │
│  │ TelemetryService│  │ Activity Sources │  │    Meters    │ │
│  │  • Custom Events│  │  • Custom Spans  │  │  • Counters  │ │
│  │  • Error Tracking│  │  • Trace Context│  │  • Histograms│ │
│  └────────┬────────┘  └────────┬─────────┘  └──────┬───────┘ │
│           │                    │                    │         │
│           └────────────────────┼────────────────────┘         │
│                                │                              │
└────────────────────────────────┼──────────────────────────────┘
                                 │
                  ┌──────────────┼──────────────┐
                  │              │              │
         ┌────────▼──────┐  ┌───▼────────┐  ┌─▼──────────────┐
         │ OpenTelemetry │  │   Aspire   │  │   Azure        │
         │   Collector   │  │  Dashboard │  │ App Insights   │
         │   (OTLP)      │  │   (Dev)    │  │  (Production)  │
         └───────────────┘  └────────────┘  └────────────────┘
```

### Telemetry Components

#### 1. ServiceDefaults (OpenTelemetry Configuration)

Located in `AzureAIAvatarBlazor.ServiceDefaults/Extensions.cs`:

**Logging**:
```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});
```

**Metrics**:
```csharp
.WithMetrics(metrics =>
{
    metrics.AddAspNetCoreInstrumentation()      // HTTP server metrics
        .AddHttpClientInstrumentation()          // HTTP client metrics
        .AddRuntimeInstrumentation()             // .NET runtime metrics
        .AddMeter("AzureAIAvatarBlazor");       // Custom metrics
})
```

**Tracing**:
```csharp
.WithTracing(tracing =>
{
    tracing.AddSource("AzureAIAvatarBlazor")    // Custom traces
        .AddAspNetCoreInstrumentation()          // HTTP request traces
        .AddHttpClientInstrumentation();         // HTTP client traces
})
```

**Exporters**:
- **OTLP Exporter**: Sends to Aspire Dashboard (development)
- **Azure Monitor Exporter**: Sends to Application Insights (when configured)

#### 2. TelemetryService (Custom Telemetry)

Located in `Services/TelemetryService.cs`:

**Custom Metrics**:

| Metric Name | Type | Description | Tags |
|------------|------|-------------|------|
| `avatar.sessions.started` | Counter | Avatar sessions initiated | character, style, is_custom |
| `chat.messages.sent` | Counter | Chat messages by role | role |
| `ai.response.duration` | Histogram | AI response time in ms | mode, tokens |
| `avatar.session.duration` | Histogram | Session duration in seconds | character |

**Custom Activities (Spans)**:

| Activity Name | Description | Tags |
|--------------|-------------|------|
| `GetChatCompletion` | AI chat completion operation | mode, message_length, chunks, duration_ms |
| `TestConnection` | Connection validation | mode, success, error |

**Event Tracking**:
- Avatar session lifecycle (start/end)
- Configuration changes
- Errors with context
- Speech synthesis operations
- WebRTC connection status

#### 3. Integration Points

**AzureAIAgentService**:
```csharp
using var activity = _telemetryService.StartActivity("GetChatCompletion", ActivityKind.Client);

// Track user message
_telemetryService.TrackChatMessage("user", message.Length);

// Track AI response
_telemetryService.TrackAIResponseTime(mode, duration, tokenCount);

// Track assistant message
_telemetryService.TrackChatMessage("assistant", responseLength);
```

**ConfigurationService**:
```csharp
// Track configuration changes
_telemetryService.TrackConfigurationChange("Avatar.Character", oldValue, newValue);
```

### Viewing Telemetry

#### Development (Aspire Dashboard)

Access: https://localhost:15216

**Logs Tab**:
- Structured logs from all services
- Filter by severity, source, timestamp
- Search log content
- View log context and properties

**Metrics Tab**:
- Real-time metric charts
- Custom metrics:
  - Avatar sessions
  - Chat messages
  - AI response times
  - Session durations
- System metrics:
  - HTTP requests/responses
  - Memory usage
  - CPU usage

**Traces Tab**:
- Distributed traces across operations
- Span details with tags
- Performance waterfall
- Dependencies visualization

**Resources Tab**:
- Service health status
- Resource utilization
- Container logs

#### Production (Application Insights)

Access: Azure Portal → Application Insights resource

**Application Map**:
- Service dependencies
- Request volumes
- Response times
- Failure rates

**Performance**:
- Request performance
- Dependency performance
- Custom metrics
- Percentile charts

**Failures**:
- Exception tracking
- Failure rates by operation
- Stack traces
- Affected users

**Logs (KQL Queries)**:

View avatar sessions:
```kql
traces
| where message contains "Avatar session"
| project timestamp, message, customDimensions
| order by timestamp desc
```

View AI response times:
```kql
customMetrics
| where name == "ai.response.duration"
| summarize avg(value), percentiles(value, 50, 90, 99) by bin(timestamp, 5m)
| render timechart
```

View configuration changes:
```kql
traces
| where message contains "Configuration changed"
| project timestamp, message, customDimensions
| order by timestamp desc
```

### Telemetry Best Practices

1. **Structured Logging**: Use structured log messages with parameters
   ```csharp
   _logger.LogInformation("Avatar session started: Character={Character}", character);
   ```

2. **Custom Spans**: Wrap important operations in activities
   ```csharp
   using var activity = _telemetryService.StartActivity("OperationName");
   activity?.SetTag("key", "value");
   ```

3. **Metrics Over Logs**: Use metrics for counters and measurements
   ```csharp
   _telemetryService.TrackAvatarSessionStart(character, style, isCustom);
   ```

4. **Error Context**: Always include context when logging errors
   ```csharp
   _telemetryService.TrackError("OperationName", exception);
   ```

5. **Performance Tags**: Add relevant tags to spans for filtering
   ```csharp
   activity?.SetTag("mode", mode);
   activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
   ```

### Configuration

#### AppHost (Development)

```bash
# Optional: Configure Application Insights
dotnet user-secrets set "ConnectionStrings:appinsights" "InstrumentationKey=...;IngestionEndpoint=https://..."
```

#### Environment Variables (Production)

```bash
# Application Insights connection string
APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=...;IngestionEndpoint=https://..."

# OTLP endpoint (alternative)
OTEL_EXPORTER_OTLP_ENDPOINT="http://collector:4317"
```

If no Application Insights connection string is provided, telemetry is only sent to the Aspire Dashboard (development) or OTLP endpoint (if configured).

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
