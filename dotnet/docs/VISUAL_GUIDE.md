# Agent Framework Integration - Visual Guide

## UI Screenshots Guide

### 1. Config Page - Mode Selection

The Config page now includes a mode selector in the "Azure OpenAI / Agent Configuration" section:

```
┌────────────────────────────────────────────────────────────────┐
│ Azure OpenAI / Agent Configuration                             │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Mode:                                                          │
│ ┌──────────────────────────────────────────────────┐          │
│ │ ▼ LLM (Direct OpenAI)                            │ <-- Dropdown
│ │   Agent-LLM (Agent Framework with OpenAI)        │          │
│ │   Agent-AIFoundry (Azure AI Foundry Agent)       │          │
│ └──────────────────────────────────────────────────┘          │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 2. Config Page - LLM Mode Fields

When "LLM" mode is selected:

```
┌────────────────────────────────────────────────────────────────┐
│ Mode: LLM (Direct OpenAI)                                      │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Endpoint:                                                      │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ https://your-resource.openai.azure.com                   │  │
│ └──────────────────────────────────────────────────────────┘  │
│ Azure OpenAI resource endpoint                                │
│                                                                │
│ API Key:                                                       │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ ••••••••••••••••••••••••••••                             │  │
│ └──────────────────────────────────────────────────────────┘  │
│                                                                │
│ Deployment Name:                                               │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ gpt-4o-mini                                              │  │
│ └──────────────────────────────────────────────────────────┘  │
│                                                                │
│ System Prompt / Instructions:                                  │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ You are a helpful AI assistant...                        │  │
│ │                                                          │  │
│ └──────────────────────────────────────────────────────────┘  │
│ [↻ Reset to Default]                                          │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 3. Config Page - Agent-LLM Mode Fields

When "Agent-LLM" mode is selected (same fields as LLM):

```
┌────────────────────────────────────────────────────────────────┐
│ Mode: Agent-LLM (Agent Framework with OpenAI)                  │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Endpoint, API Key, Deployment Name, Instructions               │
│ (Same layout as LLM mode above)                               │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 4. Config Page - Agent-AIFoundry Mode Fields

When "Agent-AIFoundry" mode is selected:

```
┌────────────────────────────────────────────────────────────────┐
│ Mode: Agent-AIFoundry (Azure AI Foundry Agent)                 │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Agent ID:                                                      │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ your-agent-id-here                                       │  │
│ └──────────────────────────────────────────────────────────┘  │
│ The ID of your pre-configured Azure AI Foundry agent          │
│                                                                │
│ Endpoint:                                                      │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │ https://your-project.api.azureml.ms                      │  │
│ └──────────────────────────────────────────────────────────┘  │
│ Azure AI Foundry project endpoint                             │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 5. Chat Page - Mode Badge Display

The Chat page displays the current mode in a badge at the top:

```
┌────────────────────────────────────────────────────────────────┐
│ [Open Avatar Session] [Start Microphone] [Stop Speaking]      │
│ [Clear Chat History] [Close Avatar Session]   [🖥️ Mode: LLM] │
└────────────────────────────────────────────────────────────────┘
```

Or with Agent modes:

```
Mode Badge Examples:
┌─────────────────────┐
│ 🖥️ Mode: LLM        │  <- Blue badge
└─────────────────────┘

┌─────────────────────┐
│ 🖥️ Mode: Agent-LLM  │  <- Blue badge
└─────────────────────┘

┌──────────────────────────────┐
│ 🖥️ Mode: Agent-AIFoundry     │  <- Blue badge
└──────────────────────────────┘
```

## Architecture Flow Diagram

```
User Opens Config Page
         │
         ▼
┌─────────────────────┐
│ Select Mode         │
│ ┌─────────────────┐ │
│ │ • LLM           │ │
│ │ • Agent-LLM     │ │
│ │ • Agent-AIFoundry│ │
│ └─────────────────┘ │
└──────────┬──────────┘
           │
           ▼
Mode-Specific Fields Appear
           │
           ├─── LLM: Endpoint, Key, Deployment, Prompt
           │
           ├─── Agent-LLM: Endpoint, Key, Deployment, Instructions
           │
           └─── Agent-AIFoundry: Endpoint, Agent ID
           │
           ▼
User Fills Fields & Saves
           │
           ▼
Configuration Stored
           │
           ▼
User Opens Chat Page
           │
           ▼
Mode Badge Displayed
           │
           ▼
User Sends Message
           │
           ▼
Chat.razor Checks Mode
           │
     ┌─────┴─────┐
     │           │
     ▼           ▼
  LLM Mode    Agent Mode
     │           │
     ▼           ▼
OpenAIService  AgentService
     │           │
     │           ├─── Agent-LLM
     │           │    └─> AzureOpenAIClient
     │           │        └─> AsIChatClient()
     │           │            └─> CreateAIAgent()
     │           │
     │           └─── Agent-AIFoundry
     │                └─> PersistentAgentsClient
     │                    └─> GetAIAgentAsync()
     │           │
     └───────────┴───────────┐
                             │
                             ▼
                    Response Generated
                             │
                             ▼
                    Display in Chat UI
                             │
                             ▼
                    Avatar Speaks Response
```

## Code Flow Example

### When User Sends a Message:

```csharp
// 1. User types message and clicks Send
userMessage = "Hello, how are you?";

// 2. ProcessOutgoingMessageAsync is called
await ProcessOutgoingMessageAsync(userMessage, clearInput: true);

// 3. Message added to history
chatMessages.Add(new ChatMessage { Role = "user", Content = "Hello, how are you?" });

// 4. System checks current mode
var mode = config?.AzureOpenAI.Mode ?? "LLM";

// 5. Route to appropriate service
if (mode == "LLM")
{
    // Use direct OpenAI
    await foreach (var chunk in OpenAIService.GetChatCompletionStreamAsync(messages))
    {
        response.Append(chunk);
    }
}
else if (mode == "Agent-LLM" || mode == "Agent-AIFoundry")
{
    // Use Agent Framework
    await foreach (var chunk in AgentService.GetChatCompletionStreamAsync(messages))
    {
        response.Append(chunk);
    }
}

// 6. Display response
chatMessages.Add(new ChatMessage { Role = "assistant", Content = response.ToString() });

// 7. Avatar speaks the response
await JSRuntime.InvokeVoidAsync("speakText", response.ToString());
```

## Configuration Examples

### Example 1: LLM Mode (Environment Variables)

```bash
export AGENT_MODE="LLM"
export AZURE_OPENAI_ENDPOINT="https://myopenai.openai.azure.com"
export AZURE_OPENAI_API_KEY="abc123..."
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"
export SYSTEM_PROMPT="You are a helpful assistant."
```

### Example 2: Agent-LLM Mode (User Secrets)

```bash
dotnet user-secrets set "AzureOpenAI:Mode" "Agent-LLM"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://myopenai.openai.azure.com"
dotnet user-secrets set "AzureOpenAI:ApiKey" "abc123..."
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
dotnet user-secrets set "AzureOpenAI:SystemPrompt" "You are a helpful agent."
```

### Example 3: Agent-AIFoundry Mode (User Secrets)

```bash
dotnet user-secrets set "AzureOpenAI:Mode" "Agent-AIFoundry"
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://myproject.api.azureml.ms"
dotnet user-secrets set "AzureOpenAI:AgentId" "asst_123abc..."
```

## Validation Messages

The system provides helpful validation messages:

```
LLM Mode:
✗ "Azure OpenAI endpoint is required."
✗ "Azure OpenAI API key is required."
✗ "Azure OpenAI deployment name is required."

Agent-LLM Mode:
✗ "Azure OpenAI endpoint is required for Agent-LLM mode."
✗ "Azure OpenAI API key is required for Agent-LLM mode."
✗ "Azure OpenAI deployment name is required for Agent-LLM mode."

Agent-AIFoundry Mode:
✗ "Azure AI Foundry endpoint is required for Agent-AIFoundry mode."
✗ "Agent ID is required for Agent-AIFoundry mode."

Invalid Mode:
✗ "Invalid mode 'XYZ'. Supported modes are: LLM, Agent-LLM, Agent-AIFoundry."
```

## Testing Checklist

- [ ] Config page loads without errors
- [ ] Mode dropdown shows all three options
- [ ] Selecting LLM shows: Endpoint, API Key, Deployment, Prompt
- [ ] Selecting Agent-LLM shows: Endpoint, API Key, Deployment, Instructions
- [ ] Selecting Agent-AIFoundry shows: Agent ID, Endpoint
- [ ] Saving configuration succeeds
- [ ] Chat page displays correct mode badge
- [ ] Sending message in LLM mode works
- [ ] Sending message in Agent-LLM mode works (if configured)
- [ ] Sending message in Agent-AIFoundry mode works (if configured)
- [ ] Mode switching updates UI immediately
- [ ] Validation errors appear for missing required fields
