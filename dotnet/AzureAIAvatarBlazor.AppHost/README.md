# AzureAIAvatarBlazor.AppHost

This is the .NET Aspire orchestration project that manages the Azure AI Avatar application and its dependencies.

## What is .NET Aspire?

.NET Aspire is an application orchestration framework that:

- Manages service dependencies and connection strings
- Provides a unified dashboard for monitoring
- Simplifies local development and cloud deployment
- Handles service discovery and configuration injection

## Project Structure

```
AzureAIAvatarBlazor.AppHost/
├── AppHost.cs              # Main orchestration logic
├── appsettings.json        # AppHost logging configuration
└── AzureAIAvatarBlazor.AppHost.csproj
```

## How It Works

### AppHost.cs Overview

The `AppHost.cs` file defines:

1. **Connection Strings** - Azure service endpoints and keys
2. **Project References** - The Blazor application
3. **Resource Dependencies** - Links between services and the app

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Define Azure OpenAI connection
var openai = builder.AddConnectionString("openai");

// Define Azure Speech connection
var speech = builder.AddConnectionString("speech");

// Define Microsoft Foundry connection (optional)
var microsoftfoundryproject = builder.AddConnectionString("microsoftfoundryproject");

// Add the Blazor app with references to all connections
var avatarApp = builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WithReference(openai)
    .WithReference(speech)
    .WithReference(microsoftfoundryproject);

builder.Build().Run();
```

## Configuration

Aspire uses **connection strings** in this format:

```
Endpoint=https://resource.endpoint.com/;Key=YOUR_API_KEY;
```

### Method 1: User Secrets (Recommended for Development)

```bash
cd dotnet/AzureAIAvatarBlazor.AppHost

# Configure Azure OpenAI
dotnet user-secrets set "ConnectionStrings:openai" "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_OPENAI_KEY;"

# Configure Azure Speech Service
dotnet user-secrets set "ConnectionStrings:speech" "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_SPEECH_KEY;"

# Configure Microsoft Foundry (optional - for Agent-MicrosoftFoundry mode)
dotnet user-secrets set "ConnectionStrings:microsoftfoundryproject" "https://your-project.foundry.azure.net"

# Configure application settings
dotnet user-secrets set "Avatar:Character" "lisa"
dotnet user-secrets set "Avatar:Style" "casual-sitting"
dotnet user-secrets set "OpenAI:DeploymentName" "gpt-4o-mini"
dotnet user-secrets set "SystemPrompt" "You are a helpful AI assistant."
```

### Method 2: Environment Variables

**Windows (PowerShell)**:

```powershell
$env:ConnectionStrings__openai = "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_KEY;"
$env:ConnectionStrings__speech = "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"
$env:ConnectionStrings__microsoftfoundryproject = "https://your-project.foundry.azure.net"
$env:Avatar__Character = "lisa"
$env:OpenAI__DeploymentName = "gpt-4o-mini"
```

**macOS/Linux (Bash)**:

```bash
export ConnectionStrings__openai="Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_KEY;"
export ConnectionStrings__speech="Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;"
export ConnectionStrings__microsoftfoundryproject="https://your-project.foundry.azure.net"
export Avatar__Character="lisa"
export OpenAI__DeploymentName="gpt-4o-mini"
```

### Method 3: appsettings.Development.json (Not Recommended - gitignored)

```json
{
  "ConnectionStrings": {
    "openai": "Endpoint=https://YOUR_RESOURCE.openai.azure.com/;Key=YOUR_KEY;",
    "speech": "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key=YOUR_KEY;",
    "microsoftfoundryproject": "https://your-project.foundry.azure.net"
  },
  "Avatar": {
    "Character": "lisa",
    "Style": "casual-sitting"
  },
  "OpenAI": {
    "DeploymentName": "gpt-4o-mini"
  },
  "SystemPrompt": "You are a helpful AI assistant."
}
```

## Running the Application

### From Command Line

```bash
cd dotnet/AzureAIAvatarBlazor.AppHost
dotnet run
```

### From VS Code

1. Open the repository in VS Code
2. Press **Ctrl+Shift+B** (or Cmd+Shift+B on macOS)
3. Or use **Tasks: Run Task** → "Aspire: Run"

### From Visual Studio 2022

1. Open `dotnet/AzureAIAvatarBlazor.slnx`
2. Set `AzureAIAvatarBlazor.AppHost` as startup project
3. Press **F5** to run with debugging

## Output

When you run the AppHost, you'll see:

```
Building...
info: Aspire.Hosting[0]
      Aspire Dashboard listening at: https://localhost:15216
info: Aspire.Hosting[0]
      azureaiavatarblazor listening at: https://localhost:5001
```

### Access Points

1. **Aspire Dashboard**: <https://localhost:15216>
   - Monitor all services
   - View logs, metrics, and traces
   - Check resource status

2. **Blazor Application**: <https://localhost:5001>
   - Main application UI
   - Chat interface
   - Configuration page

## How Configuration Flows

1. **AppHost** reads connection strings from User Secrets or environment variables
2. **Aspire** injects these as `ConnectionStrings:openai`, `ConnectionStrings:speech`, etc.
3. **ConfigurationService.cs** in the Blazor app:
   - Parses the connection string format (`Endpoint=...;Key=...;`)
   - Extracts endpoint and key values
   - Falls back to individual config values if connection strings aren't available
4. **Services** use the configuration to connect to Azure resources

## Connection String Parsing

The Blazor app's `ConfigurationService.cs` includes logic to parse Aspire connection strings:

```csharp
// Aspire connection string format: "Endpoint=https://...;Key=...;"
var openaiConnectionString = _configuration.GetConnectionString("openai");
var (openaiEndpoint, openaiKey) = ParseConnectionString(openaiConnectionString ?? "");

// Use parsed values with fallback to individual config
Endpoint = openaiEndpoint
    ?? _configuration["AZURE_OPENAI_ENDPOINT"]
    ?? _configuration["AzureOpenAI:Endpoint"]
    ?? string.Empty,
ApiKey = openaiKey
    ?? _configuration["AZURE_OPENAI_API_KEY"]
    ?? _configuration["AzureOpenAI:ApiKey"]
    ?? string.Empty
```

## Microsoft Foundry (Optional)

Microsoft Foundry connection is optional and only used when:

- Mode is set to `"Agent-MicrosoftFoundry"` in configuration
- `ConnectionStrings:microsoftfoundryproject` is configured

Unlike Azure OpenAI and Speech, the Foundry connection string is just a URL (no parsing needed):

```
https://your-project.foundry.azure.net
```

## Deployment

For production deployment using Azure Developer CLI:

```bash
cd dotnet/AzureAIAvatarBlazor.AppHost

# Initialize
azd init

# Login to Azure
azd auth login

# Deploy
azd up
```

Aspire will automatically:

- ✅ Create resource groups
- ✅ Provision Azure OpenAI + deploy models
- ✅ Provision Azure Speech Service
- ✅ Create Azure Container Apps environment
- ✅ Deploy the Blazor app as a container
- ✅ Configure managed identities
- ✅ Set up networking and DNS

## Troubleshooting

### "Connection string not found"

**Cause**: Missing `ConnectionStrings:openai` or `ConnectionStrings:speech` configuration

**Fix**: Configure using User Secrets or environment variables (see Configuration section above)

### "Endpoint cannot be null"

**Cause**: Connection string is set but in wrong format

**Fix**: Use the exact format: `Endpoint=https://...;Key=...;`

### Port already in use

**Cause**: Another Aspire or Blazor instance is running

**Fix**:

- Stop other instances
- Or change ports in Aspire Dashboard settings

### Microsoft Foundry agent not found

**Cause**: Optional - this is only needed for `Agent-MicrosoftFoundry` mode

**Fix**:

- Either configure the connection string if you want to use Microsoft Foundry
- Or use `Agent-LLM` mode (default) which only requires OpenAI and Speech

## Architecture

```
┌──────────────────────────────────────────────┐
│      AzureAIAvatarBlazor.AppHost             │
│  • Manages connection strings                │
│  • Orchestrates Blazor app                   │
│  • Provides Aspire Dashboard                 │
└─────────┬────────────────────────────────────┘
          │ Injects ConnectionStrings
          │
┌─────────▼────────────────────────────────────┐
│      AzureAIAvatarBlazor                     │
│  • Reads ConnectionStrings from config       │
│  • Parses Aspire format                      │
│  • Connects to Azure services                │
└─────────┬────────────────────────────────────┘
          │
    ┌─────┼─────┬──────────────────────┐
    │     │     │                      │
    ▼     ▼     ▼                      ▼
┌────┐ ┌────┐ ┌──────┐        ┌────────────┐
│Azure│ │Azure│ │Microsoft│   │MAFLocal    │
│OpenAI│ │Speech│ │Foundry  │   │MAFFoundry  │
└────┘ └────┘ └──────┘        └────────────┘
```

## Key Files

- **AppHost.cs**: Orchestration logic (defines resources)
- **AzureAIAvatarBlazor/Services/ConfigurationService.cs**: Parses connection strings
- **AzureAIAvatarBlazor/Program.cs**: Registers MAF agents

## Dependencies

- **Aspire.Hosting.AppHost** (13.1.0) - Core Aspire hosting
- **Aspire.Hosting.Azure.CognitiveServices** (13.0.0) - Azure AI service support

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Connection String Configuration](https://learn.microsoft.com/dotnet/aspire/fundamentals/external-parameters)
