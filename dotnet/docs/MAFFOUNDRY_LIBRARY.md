# Microsoft Foundry Integration Library

## Overview

The `AzureAIAvatarBlazor.MAFFoundry` class library provides integration with Microsoft Foundry projects, enabling the application to use pre-deployed agents, chat clients, and embedding generators from Azure AI Foundry.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    AppHost Configuration                     │
│  • microsoftfoundryproject connection string                │
│  • tenantId connection string                               │
│  • Passed to Blazor app via WithReference()                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              AzureAIAvatarBlazor.MAFFoundry                  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         MAFFoundryAgentProvider                        │ │
│  │  • AIProjectClient from Azure.AI.Projects             │ │
│  │  • DefaultAzureCredential with TenantId               │ │
│  │  • GetChatClient() → IChatClient                      │ │
│  │  • GetEmbeddingGenerator() → IEmbeddingGenerator      │ │
│  │  • GetAIAgent() → AIAgent                             │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  Blazor App (Program.cs)                     │
│  • builder.AddMAFFoundryAgents()                            │
│  • Registers: IChatClient, IEmbeddingGenerator              │
│  • Registers: MAFFoundryAgentProvider (singleton)           │
└─────────────────────────────────────────────────────────────┘
```

## Key Classes

### MAFFoundryAgentProvider

Core provider class that manages connections to Microsoft Foundry projects.

**Constructor:**
```csharp
public MAFFoundryAgentProvider(
    string microsoftFoundryProjectEndpoint, 
    IConfiguration configuration, 
    string tenantId = "")
```

**Methods:**

| Method | Description | Returns |
|--------|-------------|---------|
| `GetChatClient(deploymentName?)` | Gets an IChatClient for the specified deployment | `IChatClient` |
| `GetEmbeddingGenerator(deploymentName?)` | Gets an embedding generator | `IEmbeddingGenerator<string, Embedding<float>>` |
| `GetAIAgent(agentName, tools?)` | Gets an existing agent by name | `AIAgent` |
| `GetOrCreateAIAgent(...)` | Gets or creates an agent | `AIAgent` |

**Default Deployment Names:**
- Chat: `gpt-5-mini` (configurable via `AI_ChatDeploymentName`)
- Embeddings: `text-embedding-3-small` (configurable via `AI_embeddingsDeploymentName`)

### MAFFoundryAgentExtensions

Extension methods for registering Microsoft Foundry services in dependency injection.

**Usage:**
```csharp
builder.AddMAFFoundryAgents();
```

**What it does:**
1. Reads `ConnectionStrings:microsoftfoundryproject` from configuration
2. Reads `ConnectionStrings:tenantId` from configuration (optional)
3. If project endpoint is configured:
   - Creates `MAFFoundryAgentProvider` singleton
   - Registers `IChatClient` for chat operations
   - Registers `IEmbeddingGenerator<string, Embedding<float>>` for embeddings
4. If not configured, logs warning and continues (allows local runs)

## Configuration

### Development (User Secrets)

```bash
cd dotnet/AzureAIAvatarBlazor.AppHost

# Microsoft Foundry project endpoint
dotnet user-secrets set "ConnectionStrings:microsoftfoundryproject" "https://YOUR_PROJECT.services.ai.azure.com/api/projects/YOUR_PROJECT_ID"

# Azure Tenant ID (optional)
dotnet user-secrets set "ConnectionStrings:tenantId" "YOUR_TENANT_ID"
```

### Production (Environment Variables)

```bash
# Microsoft Foundry endpoint
export ConnectionStrings__microsoftfoundryproject="https://YOUR_PROJECT.services.ai.azure.com/api/projects/YOUR_PROJECT_ID"

# Tenant ID
export ConnectionStrings__tenantId="YOUR_TENANT_ID"
```

### AppHost Configuration

The AppHost automatically passes these connection strings to the Blazor app:

```csharp
// AppHost.cs
var microsoftfoundryproject = builder.AddConnectionString("microsoftfoundryproject");
var tenantId = builder.AddConnectionString("tenantId");

var avatarApp = builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WithReference(microsoftfoundryproject)
    .WithReference(tenantId)
    .WithReference(appInsights);
```

## Usage Examples

### Using IChatClient

```csharp
public class MyService
{
    private readonly IChatClient _chatClient;
    
    public MyService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }
    
    public async Task<string> GetResponseAsync(string userMessage)
    {
        var messages = new[]
        {
            new ChatMessage(ChatRole.User, userMessage)
        };
        
        var response = await _chatClient.CompleteAsync(messages);
        return response.Message.Text;
    }
}
```

### Using MAFFoundryAgentProvider Directly

```csharp
public class AgentService
{
    private readonly MAFFoundryAgentProvider _provider;
    
    public AgentService(MAFFoundryAgentProvider provider)
    {
        _provider = provider;
    }
    
    public AIAgent GetSpecificAgent(string agentName)
    {
        return _provider.GetAIAgent(agentName);
    }
    
    public IChatClient GetCustomChatClient(string deploymentName)
    {
        return _provider.GetChatClient(deploymentName);
    }
}
```

## Endpoint Normalization

The library includes automatic endpoint normalization for Microsoft Foundry URLs:

**Before:** `https://project.services.ai.azure.com/api/projects/projectid`  
**After:** `https://project.cognitiveservices.azure.com`

This ensures compatibility with Azure OpenAI SDK client initialization.

## Error Handling

### Missing Configuration

If the Microsoft Foundry endpoint is not configured:
- **Behavior**: Extension logs a warning and skips registration
- **Impact**: Application continues to run (allows local development)
- **Log Message**: `"Microsoft Foundry project endpoint not configured; skipping Foundry agent registration."`

### Authentication

The library uses `DefaultAzureCredential` which tries multiple authentication methods:
1. Environment variables
2. Managed identity
3. Visual Studio
4. Azure CLI
5. Interactive browser

If a `tenantId` is provided, it's included in the credential options.

## Package Dependencies

```xml
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251219.1" />
<PackageReference Include="Microsoft.Agents.AI.AzureAI" Version="1.0.0-preview.251219.1" />
<PackageReference Include="Microsoft.Agents.AI.Abstractions" Version="1.0.0-preview.251219.1" />
<PackageReference Include="Microsoft.Agents.AI.Hosting" Version="1.0.0-preview.251219.1" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.OpenAI" Version="1.0.0-alpha.251219.1" />
<PackageReference Include="Azure.Identity" Version="1.18.0-beta.2" />
<PackageReference Include="System.Memory.Data" Version="10.0.1" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.8.0-beta.1" />
```

## Comparison with Direct Azure OpenAI

| Feature | Microsoft Foundry | Direct Azure OpenAI |
|---------|------------------|---------------------|
| **Agent Management** | Pre-deployed agents in Foundry | Agents created in code |
| **Configuration** | Single project endpoint | Multiple resource endpoints |
| **Authentication** | Managed identity preferred | API keys or managed identity |
| **Tools & Extensions** | Managed in Foundry | Managed in code |
| **RAG Integration** | Built-in via Foundry | Manual implementation |
| **Multi-Agent** | Native support | Custom orchestration |

## Best Practices

1. **Use User Secrets in Development**: Keep credentials out of source control
2. **Use Managed Identity in Production**: Eliminate API keys
3. **Provide TenantId**: Improves authentication reliability in multi-tenant scenarios
4. **Check for Null IChatClient**: Handle cases where Foundry isn't configured
5. **Log Configuration State**: Use ILogger to track which configuration path is used

## Troubleshooting

### "Microsoft Foundry project endpoint not configured"

**Cause**: Connection string not set  
**Solution**: Configure `ConnectionStrings:microsoftfoundryproject` in user secrets or environment variables

### Authentication Errors

**Cause**: DefaultAzureCredential cannot authenticate  
**Solution**: 
- Verify `az login` is active
- Check managed identity is assigned in Azure
- Provide correct `tenantId` if in multi-tenant environment

### Agent Not Found

**Cause**: Agent name doesn't exist in Foundry project  
**Solution**: 
- Verify agent name matches exactly (case-sensitive)
- Check agent is deployed in the correct Foundry project
- Use `GetOrCreateAIAgent()` to auto-create if needed

## Future Enhancements

Potential additions to the library:
- [ ] Health check for Foundry connectivity
- [ ] Metrics for agent usage
- [ ] Caching for agent instances
- [ ] Retry policies for transient failures
- [ ] Support for agent versioning

---

**Reference**: Based on patterns from [aitour26-BRK445-building-enterprise-ready-ai-agents-with-microsoft-foundry](https://github.com/microsoft/aitour26-BRK445-building-enterprise-ready-ai-agents-with-microsoft-foundry)
