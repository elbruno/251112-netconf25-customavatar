# Agent Framework Integration - Implementation Summary

## Overview

This implementation adds support for the Microsoft Agent Framework to the Azure AI Avatar Blazor application, allowing users to switch between three different modes:

1. **LLM Mode** - Direct Azure OpenAI chat completion (original behavior)
2. **Agent-LLM Mode** - Using Agent Framework with Azure OpenAI endpoint, key, and model
3. **Agent-AIFoundry Mode** - Using Azure AI Foundry agents with an Agent ID

## Changes Made

### 1. Configuration Model Updates

**File**: `dotnet/AzureAIAvatarBlazor/Models/AvatarConfiguration.cs`

Added two new properties to `AzureOpenAIConfig`:
- `Mode` (string): Stores the selected mode ("LLM", "Agent-LLM", or "Agent-AIFoundry")
- `AgentId` (string?): Stores the Azure AI Foundry Agent ID (required for Agent-AIFoundry mode)

### 2. New Service: Azure AI Agent Service

**File**: `dotnet/AzureAIAvatarBlazor/Services/AzureAIAgentService.cs`

Created a new service that implements the Agent Framework:

```csharp
public interface IAzureAIAgentService
{
    IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<Models.ChatMessage> messages,
        CancellationToken cancellationToken = default);
}
```

**Key Features**:
- **Agent-LLM Mode**: Creates an AI Agent using `AzureOpenAIClient` and converts it via `AsIChatClient().CreateAIAgent()`
- **Agent-AIFoundry Mode**: Retrieves a pre-configured agent using `PersistentAgentsClient.GetAIAgentAsync(agentId)`
- Lazy initialization with thread-safe agent creation
- Unified interface matching the existing `IAzureOpenAIService`

### 3. Configuration Service Updates

**File**: `dotnet/AzureAIAvatarBlazor/Services/ConfigurationService.cs`

- Added support for reading `Mode` and `AgentId` from configuration
- Enhanced validation logic to support all three modes:
  - LLM: Requires endpoint, API key, and deployment name
  - Agent-LLM: Requires endpoint, API key, and deployment name
  - Agent-AIFoundry: Requires endpoint and Agent ID (API key optional, uses DefaultAzureCredential)

### 4. Chat Page Updates

**File**: `dotnet/AzureAIAvatarBlazor/Components/Pages/Chat.razor`

- Added mode badge display showing current mode (LLM, Agent-LLM, or Agent-AIFoundry)
- Modified chat processing logic to route requests to appropriate service based on mode
- Injected both `IAzureOpenAIService` and `IAzureAIAgentService`

### 5. Config Page Updates

**File**: `dotnet/AzureAIAvatarBlazor/Components/Pages/Config.razor`

Added comprehensive UI for mode selection:
- Dropdown to select mode (LLM, Agent-LLM, or Agent-AIFoundry)
- Conditional fields based on selected mode:
  - **Agent-AIFoundry**: Shows Agent ID field and project endpoint
  - **LLM/Agent-LLM**: Shows API Key, Deployment Name, and System Prompt
- Dynamic help text and placeholders based on mode

### 6. Service Registration

**File**: `dotnet/AzureAIAvatarBlazor/Program.cs`

Registered both services:
```csharp
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IAzureAIAgentService, AzureAIAgentService>();
```

### 7. NuGet Packages Added

Updated `dotnet/AzureAIAvatarBlazor/AzureAIAvatarBlazor.csproj`:
- `Azure.AI.Agents.Persistent` (Version 1.2.0-beta.7)
- `Microsoft.Agents.AI` (Version 1.0.0-preview.251028.1)
- `Microsoft.Agents.AI.AzureAI` (Version 1.0.0-preview.251028.1)
- `Azure.Identity` (Version 1.17.0)

### 8. Target Framework

Projects currently target **.NET 9.0** (downgraded from .NET 10.0 due to SDK availability)

## Configuration

### Environment Variables / User Secrets

Add the following configuration:

```bash
# For LLM or Agent-LLM modes
dotnet user-secrets set "AzureOpenAI:Mode" "LLM"  # or "Agent-LLM"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"

# For Agent-AIFoundry mode
dotnet user-secrets set "AzureOpenAI:Mode" "Agent-AIFoundry"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-project.api.azureml.ms"
dotnet user-secrets set "AzureOpenAI:AgentId" "your-agent-id"
```

Or using environment variables:
```bash
export AGENT_MODE="Agent-LLM"
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
export AZURE_OPENAI_API_KEY="your-api-key"
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"

# For Agent-AIFoundry
export AGENT_MODE="Agent-AIFoundry"
export AGENT_ID="your-agent-id"
```

## Usage

### Via UI (Config Page)

1. Navigate to the Config page
2. In the "Azure OpenAI / Agent Configuration" section:
   - Select your desired mode from the dropdown
   - Fill in the required fields based on the mode:
     - **LLM**: Endpoint, API Key, Deployment Name, System Prompt
     - **Agent-LLM**: Endpoint, API Key, Deployment Name, Instructions
     - **Agent-AIFoundry**: Endpoint, Agent ID
3. Save the configuration
4. Navigate to the Chat page
5. The mode badge will display the current mode
6. Start a conversation - the system will use the selected mode

### Programmatically

The system automatically selects the appropriate service based on `config.AzureOpenAI.Mode`:

```csharp
var mode = config?.AzureOpenAI.Mode ?? "LLM";

if (mode == "LLM")
{
    // Use IAzureOpenAIService
    await foreach (var chunk in OpenAIService.GetChatCompletionStreamAsync(messages))
    {
        // Process chunk
    }
}
else if (mode == "Agent-LLM" || mode == "Agent-AIFoundry")
{
    // Use IAzureAIAgentService
    await foreach (var chunk in AgentService.GetChatCompletionStreamAsync(messages))
    {
        // Process chunk
    }
}
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Chat.razor (UI)                         │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Mode Display Badge (LLM / Agent-LLM / Agent-AIFoundry) │  │
│  └──────────────────────────────────────────────────────┘  │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│          ConfigurationService.GetConfiguration()            │
│                     (reads Mode)                            │
└───────────────────┬─────────────────────────────────────────┘
                    │
        ┌───────────┴───────────┐
        │                       │
        ▼                       ▼
┌───────────────┐       ┌──────────────────┐
│  Mode = LLM   │       │  Mode = Agent-*  │
└───────┬───────┘       └────────┬─────────┘
        │                        │
        ▼                        ▼
┌──────────────────────┐  ┌────────────────────────┐
│ IAzureOpenAIService  │  │ IAzureAIAgentService   │
│                      │  │                        │
│ - Uses Azure.AI      │  │ - Uses Microsoft.      │
│   .OpenAI SDK        │  │   Agents.AI            │
│ - Direct chat        │  │ - Agent Framework      │
│   completion         │  │ - RunAsync()           │
└──────────────────────┘  └────────────────────────┘
                                 │
                 ┌───────────────┴────────────────┐
                 │                                │
                 ▼                                ▼
    ┌────────────────────────┐    ┌────────────────────────┐
    │  Agent-LLM             │    │  Agent-AIFoundry       │
    │                        │    │                        │
    │  - AzureOpenAIClient   │    │  - PersistentAgents    │
    │  - .AsIChatClient()    │    │    Client              │
    │  - .CreateAIAgent()    │    │  - .GetAIAgentAsync()  │
    └────────────────────────┘    └────────────────────────┘
```

## Benefits

1. **Flexibility**: Users can choose the most appropriate mode for their use case
2. **Agent Features**: Leverage advanced Agent Framework capabilities (workflows, tools, etc.)
3. **AI Foundry Integration**: Use pre-configured agents from Azure AI Foundry
4. **Backward Compatible**: Existing LLM mode works exactly as before
5. **Unified Interface**: All modes use the same `GetChatCompletionStreamAsync` interface

## Testing

To test each mode:

### 1. Test LLM Mode
```bash
dotnet user-secrets set "AzureOpenAI:Mode" "LLM"
dotnet run --project dotnet/AzureAIAvatarBlazor.AppHost
```
Navigate to Config page, verify fields, then test chat.

### 2. Test Agent-LLM Mode
```bash
dotnet user-secrets set "AzureOpenAI:Mode" "Agent-LLM"
dotnet run --project dotnet/AzureAIAvatarBlazor.AppHost
```
Navigate to Config page, verify fields, then test chat.

### 3. Test Agent-AIFoundry Mode
```bash
dotnet user-secrets set "AzureOpenAI:Mode" "Agent-AIFoundry"
dotnet user-secrets set "AzureOpenAI:AgentId" "your-agent-id"
dotnet run --project dotnet/AzureAIAvatarBlazor.AppHost
```
Navigate to Config page, verify Agent ID field shows, then test chat.

## Known Limitations

1. **Streaming**: Agent Framework's `RunAsync()` currently returns full response, not streaming chunks like OpenAI SDK
2. **Conversation History**: Agent-based modes only send the last user message to the agent (by design of Agent Framework)
3. **.NET 10**: Projects currently target .NET 9 due to SDK availability. Upgrade to .NET 10 when SDK becomes available.
4. **Authentication**: Agent-AIFoundry mode uses `DefaultAzureCredential` for authentication (Azure CLI, Managed Identity, etc.)

## References

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/)
- [Azure AI Foundry Agent Sample](https://github.com/microsoft/Generative-AI-for-beginners-dotnet/blob/main/samples/AgentFx/AgentFx-AIFoundryAgents-01/Program.cs)
- [LLM-based Agent Sample](https://github.com/microsoft/Generative-AI-for-beginners-dotnet/blob/main/samples/AgentFx/AgentFx-AIFoundry-02/Program.cs)
