# Azure AI Avatar - Aspire Migration Plan

## Executive Summary

This plan outlines the complete migration of the .NET Blazor application to use .NET Aspire for:

- **Centralized AI resource management** via AppHost
- **Elimination of appsettings.json dependency** in favor of Aspire-managed configuration
- **Improved local development** with automatic resource provisioning
- **Updated documentation** reflecting the new architecture
- **Enhanced developer experience** with Aspire CLI integration

---

## 📋 Table of Contents

1. [Prerequisites](#prerequisites)
2. [Phase 1: Update AppHost with AI Resources](#phase-1-update-apphost-with-ai-resources)
3. [Phase 2: Add Aspire Client Integration](#phase-2-add-aspire-client-integration)
4. [Phase 3: Remove appsettings.json Dependencies](#phase-3-remove-appsettingsjson-dependencies)
5. [Phase 4: Update Documentation](#phase-4-update-documentation)
6. [Phase 5: Add Aspire CLI Task](#phase-5-add-aspire-cli-task)
7. [Testing & Validation](#testing--validation)
8. [Rollback Strategy](#rollback-strategy)

---

## Prerequisites

### Required Tools

- ✅ .NET 9 SDK (9.0.100 or later)
- ✅ Aspire Workload: `dotnet workload install aspire`
- ✅ Aspire CLI (latest): `dotnet tool install -g aspire`
- ✅ Azure subscription with:
  - Azure Speech Service resource
  - Azure OpenAI resource (with deployed model)
  - (Optional) Azure Cognitive Search resource

### Development Environment

- Visual Studio 2022 (17.12+) or VS Code with C# DevKit
- Git for version control
- Azure CLI (optional, for provisioning)

---

## Phase 1: Update AppHost with AI Resources

### Objective

Transform the AppHost from a simple project reference to a fully-featured Aspire orchestrator with AI resource management.

### 1.1 Add Required NuGet Packages to AppHost

**File**: `dotnet/AzureAIAvatarBlazor.AppHost/AzureAIAvatarBlazor.AppHost.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UserSecretsId>f2851c1d-ae2d-45d5-b962-dcd794eff3e3</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <!-- UPDATED: Latest stable version -->
    <PackageReference Include="Aspire.Hosting.AppHost" Version="9.5.2" />
    
    <!-- NEW: Azure Cognitive Services (includes OpenAI + Speech) -->
    <PackageReference Include="Aspire.Hosting.Azure.CognitiveServices" Version="9.5.2" />
    
    <!-- NEW: For Azure Search support -->
    <PackageReference Include="Aspire.Hosting.Azure.Search" Version="9.5.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\AzureAIAvatarBlazor\AzureAIAvatarBlazor.csproj" />
  </ItemGroup>

</Project>
```

**Rationale**: Latest Aspire packages (9.5.2 stable) provide full Azure AI resource support.

---

### 1.2 Configure AppHost User Secrets

**Purpose**: Store connection credentials and Azure provisioning settings locally.

#### Option A: Command Line (Recommended)

```powershell
# Navigate to AppHost project
cd dotnet/AzureAIAvatarBlazor.AppHost

# Azure OpenAI Configuration
dotnet user-secrets set "ConnectionStrings:openai" "Endpoint=https://{your-resource}.openai.azure.com/;Key={your-key};"

# Azure Speech Service Configuration
dotnet user-secrets set "ConnectionStrings:speech" "Endpoint=https://{region}.api.cognitive.microsoft.com/;Key={your-key};"

# Azure Cognitive Search (Optional)
dotnet user-secrets set "ConnectionStrings:search" "Endpoint=https://{your-search}.search.windows.net/;Key={your-key};"

# Azure Provisioning (for automatic resource creation - optional)
dotnet user-secrets set "Azure:SubscriptionId" "{your-subscription-id}"
dotnet user-secrets set "Azure:ResourceGroupPrefix" "rg-avatar"
dotnet user-secrets set "Azure:Location" "westus2"

# Application-specific settings
dotnet user-secrets set "Avatar:Character" "lisa"
dotnet user-secrets set "Avatar:Style" "casual-sitting"
dotnet user-secrets set "OpenAI:DeploymentName" "gpt-4o-mini"
dotnet user-secrets set "SystemPrompt" "You are a helpful AI assistant with a friendly personality."
```

#### Option B: JSON File (Development)

**File**: `dotnet/AzureAIAvatarBlazor.AppHost/appsettings.Development.json` (gitignored)

```json
{
  "ConnectionStrings": {
    "openai": "Endpoint=https://{your-resource}.openai.azure.com/;Key={your-key};",
    "speech": "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key={your-key};"
  },
  "Azure": {
    "SubscriptionId": "{subscription-id}",
    "ResourceGroupPrefix": "rg-avatar",
    "Location": "westus2"
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

---

### 1.3 Rewrite AppHost.cs

**File**: `dotnet/AzureAIAvatarBlazor.AppHost/AppHost.cs`

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// ==============================
// Azure AI Resource Definitions
// ==============================

// Azure OpenAI
// Conditional: Use provisioned resource in publish mode, connection string in dev
var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
           .AddDeployment(new("chat", "gpt-4o-mini", "2024-11-01", "GlobalStandard"))
    : builder.AddConnectionString("openai");

// Azure Speech Service
// Note: Aspire doesn't have first-class Speech resource yet, use generic cognitive service
var speech = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureCognitiveServices("speech")
           .WithParameter("kind", "SpeechServices")
           .WithParameter("sku", "S0")
    : builder.AddConnectionString("speech");

// Azure Cognitive Search (Optional - only if configured)
var searchEndpoint = builder.Configuration["ConnectionStrings:search"];
IResourceBuilder<IResourceWithConnectionString>? search = null;
if (!string.IsNullOrEmpty(searchEndpoint))
{
    search = builder.ExecutionContext.IsPublishMode
        ? builder.AddAzureSearch("search")
        : builder.AddConnectionString("search");
}

// ==============================
// Application Project
// ==============================

var avatarApp = builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WithReference(openai)
    .WithReference(speech)
    .WithEnvironment("Avatar__Character", builder.Configuration["Avatar:Character"] ?? "lisa")
    .WithEnvironment("Avatar__Style", builder.Configuration["Avatar:Style"] ?? "casual-sitting")
    .WithEnvironment("OpenAI__DeploymentName", builder.Configuration["OpenAI:DeploymentName"] ?? "gpt-4o-mini")
    .WithEnvironment("SystemPrompt", builder.Configuration["SystemPrompt"] ?? "You are a helpful AI assistant.");

// Add search reference if configured
if (search != null)
{
    avatarApp.WithReference(search);
}

// ==============================
// Build and Run
// ==============================

builder.Build().Run();
```

**Key Features**:

- **Dual-mode configuration**: Dev uses connection strings, publish mode provisions Azure resources
- **Type-safe resource definitions**: Leverages Aspire's Azure resource builders
- **Environment variable injection**: Passes configuration to the Blazor app
- **Conditional Search support**: Only references Search if configured

---

### 1.4 Update .gitignore

Ensure AppHost secrets are protected:

**File**: `.gitignore` (root)

```gitignore
# Aspire AppHost Development Settings
dotnet/AzureAIAvatarBlazor.AppHost/appsettings.Development.json
dotnet/AzureAIAvatarBlazor.AppHost/appsettings.Local.json
```

---

## Phase 2: Add Aspire Client Integration

### Objective

Replace manual Azure SDK client instantiation with Aspire-managed clients in the Blazor app.

### 2.1 Update Blazor App NuGet Packages

**File**: `dotnet/AzureAIAvatarBlazor/AzureAIAvatarBlazor.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>5f415b33-a8c4-4693-a7e0-8de94e380971</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <!-- REMOVED: Direct Azure SDK packages - now managed by Aspire -->
    <!-- <PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" /> -->
    
    <!-- NEW: Aspire client integration packages -->
    <PackageReference Include="Aspire.Azure.AI.OpenAI" Version="9.5.2-preview.1.25522.3" />
    
    <!-- KEEP: Speech SDK (no Aspire component yet, use manual registration) -->
    <PackageReference Include="Microsoft.CognitiveServices.Speech" Version="1.46.0" />
    
    <!-- UPDATED: Latest Search SDK -->
    <PackageReference Include="Azure.Search.Documents" Version="11.7.0" />
    
    <!-- REMOVED: No longer need explicit user secrets (managed by AppHost) -->
    <!-- <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.10" /> -->
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\AzureAIAvatarBlazor.ServiceDefaults\AzureAIAvatarBlazor.ServiceDefaults.csproj" />
  </ItemGroup>

</Project>
```

**Changes**:

- ✅ Added `Aspire.Azure.AI.OpenAI` (preview - latest version with streaming support)
- ✅ Removed direct `Azure.AI.OpenAI` package (now managed by Aspire)
- ✅ Removed `UserSecrets` package (AppHost manages secrets)
- ⚠️ Keep `Microsoft.CognitiveServices.Speech` (no Aspire component exists yet)

---

### 2.2 Update Program.cs

**File**: `dotnet/AzureAIAvatarBlazor/Program.cs`

```csharp
using AzureAIAvatarBlazor.Components;
using AzureAIAvatarBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// Aspire Service Defaults
// ==============================
builder.AddServiceDefaults();

// ==============================
// Aspire-Managed Clients
// ==============================

// Azure OpenAI Client (automatically configured from AppHost)
builder.AddAzureOpenAIClient("openai");

// Note: Speech Service doesn't have Aspire component yet
// Configuration is injected via environment variables from AppHost

// ==============================
// Blazor Configuration
// ==============================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ==============================
// Application Services
// ==============================
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IAzureSpeechService, AzureSpeechService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

var app = builder.Build();

// ==============================
// Aspire Endpoints
// ==============================
app.MapDefaultEndpoints();

// ==============================
// Middleware Pipeline
// ==============================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

**Changes**:

- ✅ Added `builder.AddAzureOpenAIClient("openai")` - registers `AzureOpenAIClient` from AppHost connection
- ✅ Removed manual `AddUserSecrets` call
- ✅ Speech Service still configured via environment variables (no Aspire component)

---

### 2.3 Update AzureOpenAIService

**File**: `dotnet/AzureAIAvatarBlazor/Services/AzureOpenAIService.cs`

**Old Implementation** (manual client creation):

```csharp
// OLD - Don't use this anymore
var endpoint = new Uri(_configuration["AzureOpenAI:Endpoint"]);
var credential = new AzureKeyCredential(_configuration["AzureOpenAI:ApiKey"]);
var client = new AzureOpenAIClient(endpoint, credential);
```

**NEW Implementation** (Aspire-managed):

```csharp
using Azure.AI.OpenAI;
using AzureAIAvatarBlazor.Models;

namespace AzureAIAvatarBlazor.Services;

public interface IAzureOpenAIService
{
    IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<ChatMessage> messages, 
        string? systemPrompt = null, 
        CancellationToken cancellationToken = default);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly AzureOpenAIClient _client; // Injected by Aspire
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAIService> _logger;

    // Aspire automatically injects the client
    public AzureOpenAIService(
        AzureOpenAIClient client, // <-- Injected by AddAzureOpenAIClient
        IConfiguration configuration,
        ILogger<AzureOpenAIService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<ChatMessage> messages,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get deployment name from environment (injected by AppHost)
        var deploymentName = _configuration["OpenAI__DeploymentName"] 
            ?? _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] 
            ?? "gpt-4o-mini";

        var systemMessage = systemPrompt 
            ?? _configuration["SystemPrompt"] 
            ?? "You are a helpful AI assistant.";

        _logger.LogInformation("Starting chat completion with deployment: {Deployment}", deploymentName);

        var chatMessages = new List<Azure.AI.OpenAI.ChatMessage>
        {
            new Azure.AI.OpenAI.ChatMessage(ChatRole.System, systemMessage)
        };

        foreach (var msg in messages)
        {
            chatMessages.Add(new Azure.AI.OpenAI.ChatMessage(
                msg.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                msg.Content
            ));
        }

        var chatClient = _client.GetChatClient(deploymentName);
        
        await foreach (var update in chatClient.CompleteChatStreamingAsync(
            chatMessages, 
            cancellationToken: cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                yield return contentPart.Text;
            }
        }
    }
}
```

**Key Changes**:

- ✅ `AzureOpenAIClient` is now constructor-injected (no manual instantiation)
- ✅ Credentials are managed entirely by Aspire
- ✅ Deployment name read from environment variables (set by AppHost)
- ✅ No more hardcoded endpoints or API keys

---

### 2.4 Update AzureSpeechService

**File**: `dotnet/AzureAIAvatarBlazor/Services/AzureSpeechService.cs`

```csharp
using Microsoft.CognitiveServices.Speech;

namespace AzureAIAvatarBlazor.Services;

public interface IAzureSpeechService
{
    Task<bool> ValidateConnectionAsync();
    string GetRegion();
    string GetSubscriptionKey();
    string? GetPrivateEndpoint();
    bool IsPrivateEndpointEnabled();
}

public class AzureSpeechService : IAzureSpeechService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureSpeechService> _logger;

    public AzureSpeechService(
        IConfiguration configuration,
        ILogger<AzureSpeechService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GetRegion()
    {
        // Priority: Environment variable (from AppHost) > Fallback
        return _configuration["AZURE_SPEECH_REGION"] 
            ?? _configuration["AzureSpeech__Region"] 
            ?? "westus2";
    }

    public string GetSubscriptionKey()
    {
        // Extract key from ConnectionString or direct config
        var connectionString = _configuration["ConnectionStrings:speech"];
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Parse Aspire connection string: "Endpoint=...;Key=...;"
            var keyMatch = System.Text.RegularExpressions.Regex.Match(
                connectionString, 
                @"Key=([^;]+)"
            );
            if (keyMatch.Success)
            {
                return keyMatch.Groups[1].Value;
            }
        }

        // Fallback to environment variables
        return _configuration["AZURE_SPEECH_API_KEY"] 
            ?? _configuration["AzureSpeech__ApiKey"] 
            ?? string.Empty;
    }

    public string? GetPrivateEndpoint()
    {
        return _configuration["AZURE_SPEECH_PRIVATE_ENDPOINT"] 
            ?? _configuration["AzureSpeech__PrivateEndpoint"];
    }

    public bool IsPrivateEndpointEnabled()
    {
        var enabled = _configuration["AZURE_SPEECH_ENABLE_PRIVATE_ENDPOINT"] 
            ?? _configuration["AzureSpeech__EnablePrivateEndpoint"];
        
        if (!string.IsNullOrEmpty(enabled))
        {
            return bool.Parse(enabled);
        }

        // Auto-detect if private endpoint is configured
        return !string.IsNullOrEmpty(GetPrivateEndpoint());
    }

    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            var config = SpeechConfig.FromSubscription(GetSubscriptionKey(), GetRegion());
            
            // Try to create a synthesizer to validate credentials
            using var synthesizer = new SpeechSynthesizer(config);
            
            _logger.LogInformation("Speech Service connection validated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Speech Service connection");
            return false;
        }
    }
}
```

**Key Changes**:

- ✅ Reads from `ConnectionStrings:speech` (Aspire-managed)
- ✅ Parses Aspire connection string format (`Key=...;`)
- ✅ Falls back to environment variables for compatibility
- ⚠️ Still manual instantiation (no Aspire component for Speech yet)

---

### 2.5 Update ConfigurationService

**File**: `dotnet/AzureAIAvatarBlazor/Services/ConfigurationService.cs`

**CRITICAL CHANGE**: Remove all `appsettings.json` parsing logic. Configuration now comes from:

1. **AppHost environment variables** (primary)
2. **User secrets** (for testing without AppHost)

```csharp
using AzureAIAvatarBlazor.Models;
using System.Text.Json;

namespace AzureAIAvatarBlazor.Services;

public interface IConfigurationService
{
    AvatarConfiguration GetConfiguration();
    Task SaveConfigurationAsync(AvatarConfiguration config);
    Task<List<PromptProfile>> GetPromptProfilesAsync();
    Task<string> GetPromptProfileContentAsync(string fileName);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ConfigurationService> _logger;
    private AvatarConfiguration? _cachedConfig;

    public ConfigurationService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<ConfigurationService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public AvatarConfiguration GetConfiguration()
    {
        _logger.LogInformation("Loading configuration from AppHost environment...");

        // All values injected by AppHost via environment variables
        var config = new AvatarConfiguration
        {
            AzureSpeech = new AzureSpeechConfig
            {
                Region = _configuration["AZURE_SPEECH_REGION"] 
                    ?? _configuration["AzureSpeech__Region"] 
                    ?? "westus2",
                ApiKey = ExtractKeyFromConnectionString("speech") 
                    ?? _configuration["AZURE_SPEECH_API_KEY"] 
                    ?? string.Empty,
                PrivateEndpoint = _configuration["AZURE_SPEECH_PRIVATE_ENDPOINT"] 
                    ?? _configuration["AzureSpeech__PrivateEndpoint"],
                EnablePrivateEndpoint = bool.Parse(
                    _configuration["AZURE_SPEECH_ENABLE_PRIVATE_ENDPOINT"] 
                    ?? _configuration["AzureSpeech__EnablePrivateEndpoint"] 
                    ?? "false"
                )
            },
            AzureOpenAI = new AzureOpenAIConfig
            {
                Endpoint = ExtractEndpointFromConnectionString("openai") 
                    ?? _configuration["AZURE_OPENAI_ENDPOINT"] 
                    ?? string.Empty,
                ApiKey = ExtractKeyFromConnectionString("openai") 
                    ?? _configuration["AZURE_OPENAI_API_KEY"] 
                    ?? string.Empty,
                DeploymentName = _configuration["OpenAI__DeploymentName"] 
                    ?? _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] 
                    ?? "gpt-4o-mini",
                SystemPrompt = _configuration["SystemPrompt"] 
                    ?? "You are a helpful AI assistant.",
                PromptProfile = _configuration["PROMPT_PROFILE"],
                EnforcePromptProfile = bool.Parse(
                    _configuration["PROMPT_ENFORCE_PROFILE"] ?? "false"
                )
            },
            SttTts = new SttTtsConfig
            {
                SttLocales = _configuration["STT_LOCALES"] 
                    ?? "en-US,es-ES,fr-FR,de-DE",
                TtsVoice = _configuration["TTS_VOICE"] 
                    ?? "en-US-AvaMultilingualNeural",
                CustomVoiceEndpointId = _configuration["CUSTOM_VOICE_ENDPOINT_ID"],
                ContinuousConversation = bool.Parse(
                    _configuration["ENABLE_CONTINUOUS_CONVERSATION"] ?? "false"
                )
            },
            Avatar = new AvatarDisplayConfig
            {
                Character = _configuration["Avatar__Character"] 
                    ?? _configuration["AVATAR_CHARACTER"] 
                    ?? "lisa",
                Style = _configuration["Avatar__Style"] 
                    ?? _configuration["AVATAR_STYLE"] 
                    ?? "casual-sitting",
                IsCustomAvatar = DetermineIfCustomAvatar(
                    _configuration["Avatar__Character"] 
                    ?? _configuration["AVATAR_CHARACTER"] 
                    ?? "lisa"
                ),
                UseBuiltInVoice = bool.Parse(
                    _configuration["Avatar__UseBuiltInVoice"] ?? "false"
                ),
                EnableSubtitles = bool.Parse(
                    _configuration["ENABLE_SUBTITLES"] ?? "true"
                ),
                EnableAutoReconnect = bool.Parse(
                    _configuration["ENABLE_AUTO_RECONNECT"] ?? "true"
                ),
                AudioGain = double.Parse(
                    _configuration["Avatar__AudioGain"] ?? "1.8"
                )
            }
        };

        // Optional: Cognitive Search
        var searchEndpoint = ExtractEndpointFromConnectionString("search") 
            ?? _configuration["AZURE_COGNITIVE_SEARCH_ENDPOINT"];
        
        if (!string.IsNullOrEmpty(searchEndpoint))
        {
            config.AzureCognitiveSearch = new AzureCognitiveSearchConfig
            {
                Endpoint = searchEndpoint,
                ApiKey = ExtractKeyFromConnectionString("search") 
                    ?? _configuration["AZURE_COGNITIVE_SEARCH_API_KEY"] 
                    ?? string.Empty,
                IndexName = _configuration["AZURE_COGNITIVE_SEARCH_INDEX_NAME"] 
                    ?? string.Empty,
                Enabled = bool.Parse(
                    _configuration["AZURE_COGNITIVE_SEARCH_ENABLED"] ?? "false"
                )
            };
        }

        _cachedConfig = config;
        return config;
    }

    // Helper: Extract endpoint from Aspire connection string
    private string? ExtractEndpointFromConnectionString(string name)
    {
        var connectionString = _configuration[$"ConnectionStrings:{name}"];
        if (string.IsNullOrEmpty(connectionString)) return null;

        var match = System.Text.RegularExpressions.Regex.Match(
            connectionString, 
            @"Endpoint=([^;]+)"
        );
        return match.Success ? match.Groups[1].Value : null;
    }

    // Helper: Extract key from Aspire connection string
    private string? ExtractKeyFromConnectionString(string name)
    {
        var connectionString = _configuration[$"ConnectionStrings:{name}"];
        if (string.IsNullOrEmpty(connectionString)) return null;

        var match = System.Text.RegularExpressions.Regex.Match(
            connectionString, 
            @"Key=([^;]+)"
        );
        return match.Success ? match.Groups[1].Value : null;
    }

    private bool DetermineIfCustomAvatar(string character)
    {
        var standardAvatars = new[] { "lisa", "harry", "jeff", "lori", "max", "meg" };
        return !standardAvatars.Contains(character.ToLowerInvariant());
    }

    public async Task SaveConfigurationAsync(AvatarConfiguration config)
    {
        // In-memory cache only (no file writes in Aspire model)
        _cachedConfig = config;
        await Task.CompletedTask;
        _logger.LogInformation("Configuration saved to memory cache");
    }

    public async Task<List<PromptProfile>> GetPromptProfilesAsync()
    {
        try
        {
            var promptsPath = Path.Combine(
                _environment.ContentRootPath, 
                "..", "..", "prompts", "index.json"
            );

            if (!File.Exists(promptsPath))
            {
                _logger.LogWarning("Prompt profiles file not found at {Path}", promptsPath);
                return new List<PromptProfile>();
            }

            var json = await File.ReadAllTextAsync(promptsPath);
            var container = JsonSerializer.Deserialize<PromptProfilesContainer>(
                json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return container?.Profiles ?? new List<PromptProfile>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prompt profiles");
            return new List<PromptProfile>();
        }
    }

    public async Task<string> GetPromptProfileContentAsync(string fileName)
    {
        try
        {
            var promptPath = Path.Combine(
                _environment.ContentRootPath, 
                "..", "..", "prompts", fileName
            );

            if (!File.Exists(promptPath))
            {
                _logger.LogWarning("Prompt file not found at {Path}", promptPath);
                return string.Empty;
            }

            return await File.ReadAllTextAsync(promptPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prompt profile content from {FileName}", fileName);
            return string.Empty;
        }
    }
}

// DTO for deserializing prompt profiles index
internal class PromptProfilesContainer
{
    public List<PromptProfile> Profiles { get; set; } = new();
}
```

**Key Changes**:

- ✅ **NO appsettings.json parsing** - all config from environment variables
- ✅ Aspire connection string parsing (`ConnectionStrings:openai`, `ConnectionStrings:speech`)
- ✅ Backward-compatible with JavaScript-style env vars (`AZURE_SPEECH_REGION`)
- ✅ Simplified logic - single source of truth (AppHost)

---

## Phase 3: Remove appsettings.json Dependencies

### Objective

Eliminate `appsettings.json` and `appsettings.Development.json` files from the Blazor app, ensuring all configuration flows through the AppHost.

### 3.1 Delete Configuration Files

**Action**: Delete these files from `dotnet/AzureAIAvatarBlazor/`:

- ❌ `appsettings.json` (remove from project)
- ❌ `appsettings.Development.json` (remove from project)

**Verification**:

```powershell
# Ensure files are removed from git
git rm dotnet/AzureAIAvatarBlazor/appsettings.json
git rm dotnet/AzureAIAvatarBlazor/appsettings.Development.json
git commit -m "Remove appsettings files - now managed by Aspire AppHost"
```

---

### 3.2 Update .gitignore

**File**: `.gitignore` (root)

```gitignore
# .NET Aspire
**/appsettings.Development.json
**/appsettings.Local.json
**/.aspire/

# User Secrets (both AppHost and Blazor app)
**/obj/
**/bin/
```

---

### 3.3 Update Documentation References

Search and replace across all markdown files:

| Old Reference | New Reference |
|---------------|---------------|
| "Configure appsettings.json" | "Configure AppHost user secrets" |
| "appsettings.Development.json" | "AppHost environment variables" |
| "Update your appsettings" | "Set AppHost user secrets" |

**Files to update**:

- `dotnet/docs/QUICKSTART.md`
- `dotnet/docs/ARCHITECTURE.md`
- `dotnet/docs/DEPLOYMENT.md`
- Root `README.md`

---

## Phase 4: Update Documentation

### 4.1 Update QUICKSTART.md

**File**: `dotnet/docs/QUICKSTART.md`

**Section to rewrite**: "Step 3: Configure Credentials"

```markdown
### Step 3: Configure Credentials

With Aspire, all credentials are managed by the AppHost. Choose your configuration method:

#### Method A: AppHost User Secrets (Recommended for Development)

```powershell
# Navigate to AppHost project
cd dotnet/AzureAIAvatarBlazor.AppHost

# Configure Azure OpenAI
dotnet user-secrets set "ConnectionStrings:openai" "Endpoint=https://{your-resource}.openai.azure.com/;Key={your-key};"

# Configure Azure Speech Service
dotnet user-secrets set "ConnectionStrings:speech" "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key={your-key};"

# Set application defaults
dotnet user-secrets set "Avatar:Character" "lisa"
dotnet user-secrets set "Avatar:Style" "casual-sitting"
dotnet user-secrets set "OpenAI:DeploymentName" "gpt-4o-mini"
dotnet user-secrets set "SystemPrompt" "You are a helpful AI assistant."
```

#### Method B: Environment Variables (CI/CD & Production)

```powershell
# Windows PowerShell
$env:ConnectionStrings__openai = "Endpoint=https://{your-resource}.openai.azure.com/;Key={your-key};"
$env:ConnectionStrings__speech = "Endpoint=https://westus2.api.cognitive.microsoft.com/;Key={your-key};"
$env:Avatar__Character = "lisa"
$env:OpenAI__DeploymentName = "gpt-4o-mini"
```

#### Method C: Azure Provisioning (Production - Automatic)

For production deployments, Aspire can automatically provision Azure resources:

```powershell
cd dotnet/AzureAIAvatarBlazor.AppHost

# Configure Azure subscription (one-time)
dotnet user-secrets set "Azure:SubscriptionId" "{your-subscription-id}"
dotnet user-secrets set "Azure:ResourceGroupPrefix" "rg-avatar"
dotnet user-secrets set "Azure:Location" "westus2"

# Publish with automatic provisioning
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer
```

Aspire will automatically:

- ✅ Create Azure OpenAI resource
- ✅ Create Azure Speech Service resource
- ✅ Deploy GPT-4o-mini model
- ✅ Configure all connection strings
- ✅ Set up managed identities

```

---

### 4.2 Update ARCHITECTURE.md

**File**: `dotnet/docs/ARCHITECTURE.md`

**Add new section**:

```markdown
## Aspire Orchestration Layer

### Overview

.NET Aspire acts as the orchestration layer, managing:
- **Resource provisioning**: Automatic creation of Azure AI resources
- **Configuration injection**: Connection strings and environment variables
- **Service discovery**: Automatic endpoint resolution
- **Telemetry**: Unified logging, metrics, and tracing via OpenTelemetry

### Architecture Diagram (Updated)

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
```

---

### 4.3 Update DEPLOYMENT.md

**File**: `dotnet/docs/DEPLOYMENT.md`

**Replace "Option 1: Azure App Service"** with:

```markdown
### Option 1: Aspire Deployment with Azure Developer CLI (Recommended)

The easiest way to deploy is using Azure Developer CLI (azd), which leverages Aspire's built-in provisioning.

#### Step 1: Install Azure Developer CLI

```powershell
# Windows (winget)
winget install microsoft.azd

# macOS (Homebrew)
brew tap azure/azd && brew install azd

# Linux
curl -fsSL https://aka.ms/install-azd.sh | bash
```

#### Step 2: Initialize Aspire Project

```powershell
# Navigate to AppHost directory
cd dotnet/AzureAIAvatarBlazor.AppHost

# Initialize azd (one-time)
azd init

# When prompted:
# - Environment name: "avatar-demo-prod"
# - Azure location: "westus2"
```

#### Step 3: Configure Azure Subscription

```powershell
# Login to Azure
azd auth login

# Set target subscription (if you have multiple)
azd config set defaults.subscription "{subscription-id}"
```

#### Step 4: Deploy to Azure

```powershell
# Deploy everything (provision + deploy)
azd up

# This will:
# ✅ Create resource group
# ✅ Provision Azure OpenAI + deploy model
# ✅ Provision Azure Speech Service
# ✅ Create Azure Container Apps environment
# ✅ Deploy Blazor app as container
# ✅ Configure managed identities
# ✅ Set up networking and DNS
```

**Expected output**:

```
Provisioning Azure resources (azd provision)
  ✓ Provisioned resource group (rg-avatar-demo-prod)
  ✓ Provisioned Azure OpenAI (oai-avatar-demo)
  ✓ Deployed model gpt-4o-mini to Azure OpenAI
  ✓ Provisioned Speech Service (speech-avatar-demo)
  ✓ Provisioned Container Apps environment
  ✓ Configured managed identity

Deploying services (azd deploy)
  ✓ Built container image
  ✓ Pushed to Azure Container Registry
  ✓ Deployed to Azure Container Apps

SUCCESS: Your application is deployed!
  Endpoint: https://azureaiavatarblazor.{random}.azurecontainerapps.io
```

#### Step 5: Verify Deployment

```powershell
# Open the deployed app
azd show --name azureaiavatarblazor --environment avatar-demo-prod

# View logs
azd logs --name azureaiavatarblazor --environment avatar-demo-prod
```

#### Step 6: Update and Redeploy

```powershell
# After making code changes
azd deploy

# Or re-provision and deploy
azd up
```

#### Step 7: Cleanup

```powershell
# Delete all Azure resources
azd down

# Confirm when prompted
```

### Advanced: Customize Deployment

**File**: `dotnet/AzureAIAvatarBlazor.AppHost/infra/main.bicep` (auto-generated by azd)

You can customize:

- Azure regions
- SKU tiers (e.g., S0 vs Standard)
- Virtual network configuration
- Custom domains

See [Aspire Azure Provisioning Guide](https://learn.microsoft.com/dotnet/aspire/deployment/azure/azure-deployment)

```

---

## Phase 5: Add Aspire CLI Task

### Objective
Add VS Code task to launch the Aspire app using the Aspire CLI (`aspire run`).

### 5.1 Update tasks.json

**File**: `.vscode/tasks.json`

```jsonc
{
    "version": "2.0.0",
    "tasks": [
        // ==============================
        // Python Dev Server Tasks
        // ==============================
        {
            "label": "Dev Server (HTTP)",
            "type": "shell",
            "command": "powershell",
            "args": [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-Command",
                "cd \"${workspaceFolder}\\python\\dev-server\"; node server.js"
            ],
            "options": {
                "cwd": "${workspaceFolder}\\python\\dev-server"
            },
            "isBackground": true,
            "problemMatcher": []
        },
        {
            "label": "Dev Server (HTTPS)",
            "type": "shell",
            "command": "powershell",
            "args": [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-Command",
                "cd \"${workspaceFolder}\\python\\dev-server\"; $env:PORT=5173; node server.js"
            ],
            "options": {
                "cwd": "${workspaceFolder}\\python\\dev-server"
            },
            "isBackground": true,
            "problemMatcher": []
        },

        // ==============================
        // .NET Aspire Tasks (NEW)
        // ==============================
        {
            "label": "Aspire: Run (CLI)",
            "type": "shell",
            "command": "aspire",
            "args": [
                "run",
                "--project",
                "${workspaceFolder}\\dotnet\\AzureAIAvatarBlazor.AppHost\\AzureAIAvatarBlazor.AppHost.csproj"
            ],
            "options": {
                "cwd": "${workspaceFolder}\\dotnet\\AzureAIAvatarBlazor.AppHost"
            },
            "isBackground": true,
            "problemMatcher": {
                "pattern": {
                    "regexp": "^(.*)$",
                    "file": 1,
                    "location": 2,
                    "message": 3
                },
                "background": {
                    "activeOnStart": true,
                    "beginsPattern": "^Building\\.\\.\\.$",
                    "endsPattern": "^.*(Aspire Dashboard|Application started).*$"
                }
            },
            "presentation": {
                "reveal": "always",
                "panel": "dedicated",
                "clear": true,
                "echo": true
            },
            "group": {
                "kind": "build",
                "isDefault": false
            }
        },
        {
            "label": "Aspire: Run with Dashboard",
            "type": "shell",
            "command": "aspire",
            "args": [
                "run",
                "--project",
                "${workspaceFolder}\\dotnet\\AzureAIAvatarBlazor.AppHost\\AzureAIAvatarBlazor.AppHost.csproj",
                "--dashboard"
            ],
            "options": {
                "cwd": "${workspaceFolder}\\dotnet\\AzureAIAvatarBlazor.AppHost"
            },
            "isBackground": true,
            "problemMatcher": {
                "pattern": {
                    "regexp": "^(.*)$",
                    "file": 1,
                    "location": 2,
                    "message": 3
                },
                "background": {
                    "activeOnStart": true,
                    "beginsPattern": "^Building\\.\\.\\.$",
                    "endsPattern": "^.*(Aspire Dashboard|Application started).*$"
                }
            },
            "presentation": {
                "reveal": "always",
                "panel": "dedicated",
                "clear": true,
                "echo": true
            },
            "group": {
                "kind": "build",
                "isDefault": true
            }
        },
        {
            "label": "Aspire: Stop",
            "type": "shell",
            "command": "powershell",
            "args": [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-Command",
                "Get-Process -Name 'dotnet' -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*aspire*' } | Stop-Process -Force"
            ],
            "problemMatcher": []
        },
        {
            "label": "Aspire: Build",
            "type": "shell",
            "command": "dotnet",
            "args": [
                "build",
                "${workspaceFolder}\\dotnet\\AzureAIAvatarBlazor.AppHost\\AzureAIAvatarBlazor.AppHost.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile",
            "group": {
                "kind": "build",
                "isDefault": false
            }
        }
    ]
}
```

**New Tasks**:

1. **Aspire: Run (CLI)**: Uses `aspire run` to start the AppHost
2. **Aspire: Run with Dashboard**: Adds `--dashboard` flag to open Aspire Dashboard automatically
3. **Aspire: Stop**: Stops all running Aspire processes
4. **Aspire: Build**: Builds the AppHost project

**Usage**:

- Press `Ctrl+Shift+P` → "Tasks: Run Task" → "Aspire: Run with Dashboard"
- Or use keyboard shortcut `Ctrl+Shift+B` (default build task)

---

### 5.2 Add Launch Configuration (Optional)

**File**: `.vscode/launch.json` (create if doesn't exist)

```jsonc
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Aspire: Launch and Debug",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "Aspire: Build",
            "program": "dotnet",
            "args": [
                "run",
                "--project",
                "${workspaceFolder}\\dotnet\\AzureAIAvatarBlazor.AppHost\\AzureAIAvatarBlazor.AppHost.csproj"
            ],
            "cwd": "${workspaceFolder}\\dotnet\\AzureAIAvatarBlazor.AppHost",
            "stopAtEntry": false,
            "serverReadyAction": {
                "action": "openExternally",
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
                "uriFormat": "%s"
            },
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            },
            "sourceFileMap": {
                "/Views": "${workspaceFolder}/Views"
            }
        }
    ]
}
```

---

## Testing & Validation

### Test Plan

#### 1. AppHost Configuration Test

```powershell
# Navigate to AppHost
cd dotnet/AzureAIAvatarBlazor.AppHost

# Verify user secrets
dotnet user-secrets list

# Expected output:
# ConnectionStrings:openai = Endpoint=https://...
# ConnectionStrings:speech = Endpoint=https://...
# Avatar:Character = lisa
# OpenAI:DeploymentName = gpt-4o-mini
```

#### 2. Build Test

```powershell
# Build AppHost
dotnet build AzureAIAvatarBlazor.AppHost.csproj

# Build Blazor app
cd ../AzureAIAvatarBlazor
dotnet build AzureAIAvatarBlazor.csproj

# Expected: No errors
```

#### 3. Run Test (Development)

```powershell
# From AppHost directory
dotnet run --project AzureAIAvatarBlazor.AppHost.csproj

# Expected output:
# Building...
# info: Aspire.Hosting[0]
#       Aspire Dashboard listening at: https://localhost:15216
# info: Aspire.Hosting[0]
#       azureaiavatarblazor listening at: https://localhost:5001
```

**Verification checklist**:

- [ ] Aspire Dashboard opens at `https://localhost:15216`
- [ ] Blazor app accessible at `https://localhost:5001`
- [ ] Dashboard shows "azureaiavatarblazor" project as "Running"
- [ ] Dashboard shows OpenAI connection string configured
- [ ] Dashboard shows Speech connection string configured
- [ ] Browser console shows no errors on avatar session start
- [ ] Chat messages stream correctly
- [ ] Avatar video loads and speaks

#### 4. Configuration Test

Navigate to `https://localhost:5001/config`:

- [ ] Azure Speech region displays correctly (from AppHost env var)
- [ ] Azure OpenAI endpoint displays correctly (from connection string)
- [ ] Deployment name shows configured value (e.g., "gpt-4o-mini")
- [ ] Avatar character shows configured value (e.g., "lisa")
- [ ] No "missing configuration" warnings

#### 5. Service Integration Test

Test each service:

**AzureOpenAIService**:

```csharp
// Should use injected AzureOpenAIClient
// Verify in logs: "Using Azure OpenAI client from Aspire"
```

**AzureSpeechService**:

```csharp
// Should parse ConnectionStrings:speech
// Verify in logs: "Speech region: westus2" and "Using key from connection string"
```

**ConfigurationService**:

```csharp
// Should NOT read appsettings.json
// Verify in logs: "Loading configuration from AppHost environment..."
```

#### 6. VS Code Task Test

Press `Ctrl+Shift+P` → "Tasks: Run Task" → "Aspire: Run with Dashboard"

- [ ] AppHost builds and starts
- [ ] Terminal shows "Now listening on: <https://localhost:15216>"
- [ ] Aspire Dashboard opens automatically in browser
- [ ] No PowerShell errors

#### 7. Aspire Dashboard Test

Open `https://localhost:15216`:

**Resources Tab**:

- [ ] Shows "azureaiavatarblazor" project
- [ ] Shows "openai" connection string
- [ ] Shows "speech" connection string

**Console Logs Tab**:

- [ ] Shows Blazor app logs
- [ ] No errors about missing configuration

**Traces Tab**:

- [ ] Shows HTTP requests to Azure OpenAI
- [ ] Shows speech service calls (if tracing enabled)

**Metrics Tab**:

- [ ] Shows HTTP request metrics
- [ ] Shows ASP.NET Core metrics

#### 8. Deployment Test (Optional)

```powershell
# Install azd if not already installed
winget install microsoft.azd

# Initialize
cd dotnet/AzureAIAvatarBlazor.AppHost
azd init

# Provision and deploy
azd up

# Expected:
# ✓ Resource group created
# ✓ Azure OpenAI provisioned
# ✓ Model deployed
# ✓ Speech Service provisioned
# ✓ Container app deployed
# ✓ Managed identity configured
```

---

## Rollback Strategy

If migration fails, follow these steps to restore original configuration:

### 1. Restore appsettings.json Files

```powershell
# From git history
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/appsettings.json
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/appsettings.Development.json
```

### 2. Restore Original NuGet Packages

**File**: `dotnet/AzureAIAvatarBlazor/AzureAIAvatarBlazor.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
  <PackageReference Include="Azure.Search.Documents" Version="11.7.0" />
  <PackageReference Include="Microsoft.CognitiveServices.Speech" Version="1.46.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.10" />
</ItemGroup>
```

### 3. Restore Original Program.cs

```powershell
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/Program.cs
```

### 4. Restore Original Services

```powershell
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/Services/AzureOpenAIService.cs
git checkout HEAD~1 -- dotnet/AzureAIAvatarBlazor/Services/ConfigurationService.cs
```

### 5. Rebuild and Test

```powershell
dotnet restore
dotnet build
dotnet run
```

---

## Success Criteria

### Development Environment

- ✅ No `appsettings.json` files in Blazor app
- ✅ All configuration flows through AppHost
- ✅ `dotnet run` from AppHost starts both Dashboard and Blazor app
- ✅ VS Code task "Aspire: Run with Dashboard" works
- ✅ Avatar session connects successfully
- ✅ Chat messages stream from Azure OpenAI
- ✅ Speech service credentials loaded correctly

### Documentation

- ✅ QUICKSTART.md updated with AppHost user secrets steps
- ✅ ARCHITECTURE.md includes Aspire orchestration diagram
- ✅ DEPLOYMENT.md includes `azd up` instructions
- ✅ Root README.md updated with Aspire CLI prerequisites
- ✅ All references to appsettings.json removed

### Code Quality

- ✅ No hardcoded credentials or endpoints
- ✅ All services use dependency injection
- ✅ Configuration priority: AppHost env vars > user secrets > defaults
- ✅ Backward compatibility with JavaScript env var names

### Production Readiness

- ✅ `azd up` successfully provisions Azure resources
- ✅ Managed identity authentication works (no keys in production)
- ✅ Aspire Dashboard shows telemetry in production
- ✅ Deployment completes without manual Azure Portal steps

---

## Implementation Order

1. **Phase 1 (Day 1)**: Update AppHost packages and configuration
2. **Phase 2 (Day 2)**: Add Aspire client integration to Blazor app
3. **Phase 3 (Day 3)**: Remove appsettings.json and test locally
4. **Phase 4 (Day 4)**: Update all documentation
5. **Phase 5 (Day 5)**: Add VS Code tasks and final testing
6. **Phase 6 (Day 6)**: Test production deployment with `azd up`

**Total Estimated Time**: 5-6 days

---

## Package Versions Summary

### AppHost Packages

- `Aspire.Hosting.AppHost`: **9.5.2** (stable)
- `Aspire.Hosting.Azure.CognitiveServices`: **9.5.2** (stable)
- `Aspire.Hosting.Azure.Search`: **9.5.2** (stable)

### Blazor App Packages

- `Aspire.Azure.AI.OpenAI`: **9.5.2-preview.1.25522.3** (preview - latest)
- `Microsoft.CognitiveServices.Speech`: **1.46.0** (stable)
- `Azure.Search.Documents`: **11.7.0** (stable)

### ServiceDefaults

- `Aspire.Azure.ServiceDefaults`: **9.5.2** (via SDK)

---

## References

### Official Documentation

- [.NET Aspire Overview](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
- [Aspire Azure AI Integration](https://learn.microsoft.com/dotnet/aspire/azureai/azureai-openai-component)
- [Aspire CLI Reference](https://learn.microsoft.com/dotnet/aspire/cli/overview)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview)
- [Aspire Deployment to Azure](https://learn.microsoft.com/dotnet/aspire/deployment/azure/azure-deployment)

### NuGet Packages

- [Aspire.Hosting.Azure.CognitiveServices](https://www.nuget.org/packages/Aspire.Hosting.Azure.CognitiveServices)
- [Aspire.Azure.AI.OpenAI](https://www.nuget.org/packages/Aspire.Azure.AI.OpenAI)
- [Aspire.Hosting.Azure.Search](https://www.nuget.org/packages/Aspire.Hosting.Azure.Search)

### GitHub Issues & Samples

- [Aspire Samples Repository](https://github.com/dotnet/aspire-samples)
- [Azure AI Samples](https://github.com/Azure-Samples?q=aspire&type=all)

---

## Next Steps

After successful migration:

1. **Enable automatic provisioning**: Configure Azure subscription in AppHost secrets
2. **Add more resources**: Consider adding Redis for session state, SQL for chat history
3. **Implement managed identity**: Switch from API keys to Azure AD authentication
4. **Add monitoring**: Enable Application Insights via Aspire
5. **CI/CD pipeline**: Set up GitHub Actions with `azd deploy`

---

**End of Plan**
