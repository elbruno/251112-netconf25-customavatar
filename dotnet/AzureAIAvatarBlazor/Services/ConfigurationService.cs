using AzureAIAvatarBlazor.Models;
using System.Text.Json;

namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Service for managing application configuration
/// </summary>
public interface IConfigurationService
{
    AvatarConfiguration GetConfiguration();
    Task SaveConfigurationAsync(AvatarConfiguration config);
    Task<string?> ValidateConfigurationAsync(AvatarConfiguration config);
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

    private bool DetermineEnablePrivateEndpoint(IConfiguration config)
    {
        // Check if explicitly set in configuration  
        var explicitSetting = config["AZURE_SPEECH_ENABLE_PRIVATE_ENDPOINT"]
            ?? config["AzureSpeech__EnablePrivateEndpoint"]
            ?? config["AzureSpeech:EnablePrivateEndpoint"];

        _logger.LogInformation("DEBUG: EnablePrivateEndpoint explicit setting: '{Value}'", explicitSetting ?? "null");

        if (!string.IsNullOrEmpty(explicitSetting))
        {
            var enabled = bool.TryParse(explicitSetting, out var result) && result;
            _logger.LogInformation("Using explicit EnablePrivateEndpoint setting: {Enabled}", enabled);
            return enabled;
        }

        // For avatars, ALWAYS default to false
        // The standard regional endpoint works correctly for avatars
        // Private endpoints must be explicitly enabled
        _logger.LogInformation("No explicit private endpoint setting - defaulting to FALSE (standard regional endpoint)");
        return false;
    }

    private bool DetermineIfCustomAvatar(IConfiguration config)
    {
        // Check if explicitly set
        var explicitSetting = config["Avatar__IsCustomAvatar"]
            ?? config["Avatar:IsCustomAvatar"];

        _logger.LogInformation("Explicit IsCustomAvatar setting: '{Setting}'", explicitSetting ?? "null");

        if (!string.IsNullOrEmpty(explicitSetting))
        {
            var result = bool.Parse(explicitSetting);
            _logger.LogInformation("Using explicit custom avatar setting: {Result}", result);
            return result;
        }

        // Auto-detect: custom avatars typically have "-Avatar-" in their name or aren't in the standard list
        var character = config["Avatar__Character"]
            ?? config["Avatar:Character"]
            ?? config["AVATAR_CHARACTER"]
            ?? "lisa";

        var standardAvatars = new[] { "lisa", "harry", "jeff", "lori", "max", "meg" };

        _logger.LogInformation("Checking character '{Character}' against standard avatars", character);
        _logger.LogInformation("Character lowercase: '{Lower}'", character.ToLowerInvariant());

        var isStandard = standardAvatars.Contains(character.ToLowerInvariant());
        var isCustom = !isStandard;

        _logger.LogInformation("Is standard avatar: {IsStandard}, Is custom: {IsCustom}", isStandard, isCustom);

        return isCustom;
    }

    public AvatarConfiguration GetConfiguration()
    {
        // Return cached config if available (respects user changes from Config page)
        if (_cachedConfig != null)
        {
            _logger.LogInformation("Returning cached configuration (Character: {Character}, IsCustom: {IsCustom}, UseBuiltInVoice: {UseBuiltInVoice})",
                _cachedConfig.Avatar.Character,
                _cachedConfig.Avatar.IsCustomAvatar,
                _cachedConfig.Avatar.UseBuiltInVoice);
            return _cachedConfig;
        }

        _logger.LogInformation("Loading configuration from environment/appsettings...");

        var avatarCharacter = _configuration["Avatar__Character"] ?? _configuration["Avatar:Character"] ?? _configuration["AVATAR_CHARACTER"] ?? "lisa";
        _logger.LogInformation("Avatar character from config: '{Character}'", avatarCharacter);

        var config = new AvatarConfiguration
        {
            AzureSpeech = new AzureSpeechConfig
            {
                Region = _configuration["AZURE_SPEECH_REGION"]
                    ?? _configuration["AzureSpeech__Region"]
                    ?? _configuration["AzureSpeech:Region"]
                    ?? "westus2",
                ApiKey = ExtractKeyFromConnectionString("speech")
                    ?? _configuration["AZURE_SPEECH_API_KEY"]
                    ?? _configuration["AzureSpeech__ApiKey"]
                    ?? _configuration["AzureSpeech:ApiKey"]
                    ?? string.Empty,
                // DO NOT use PrivateEndpoint for avatars - always use standard regional endpoint
                PrivateEndpoint = null,
                // Force EnablePrivateEndpoint to false for avatar support
                EnablePrivateEndpoint = false
            },
            AzureOpenAI = new AzureOpenAIConfig
            {
                Endpoint = ExtractEndpointFromConnectionString("openai")
                    ?? _configuration["AZURE_OPENAI_ENDPOINT"]
                    ?? _configuration["AzureOpenAI__Endpoint"]
                    ?? _configuration["AzureOpenAI:Endpoint"]
                    ?? string.Empty,
                ApiKey = ExtractKeyFromConnectionString("openai")
                    ?? _configuration["AZURE_OPENAI_API_KEY"]
                    ?? _configuration["AzureOpenAI__ApiKey"]
                    ?? _configuration["AzureOpenAI:ApiKey"]
                    ?? string.Empty,
                DeploymentName = _configuration["OpenAI__DeploymentName"]
                    ?? _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
                    ?? _configuration["AzureOpenAI__DeploymentName"]
                    ?? _configuration["AzureOpenAI:DeploymentName"]
                    ?? string.Empty,
                SystemPrompt = _configuration["SystemPrompt"]
                    ?? _configuration["AzureOpenAI__SystemPrompt"]
                    ?? _configuration["AzureOpenAI:SystemPrompt"]
                    ?? _configuration["SYSTEM_PROMPT"]
                    ?? "You are Bruno Capuano (El Bruno). Respond in the user's language with brief answers (1-2 sentences) and a friendly, approachable tone. Convert numeric times (e.g., 08:00) to spoken format (e.g., \"eight in the morning\"). AgentCon Lima — November 8, 2025 (UPC Monterrico). Agenda (summary): 08:00 Registration; 08:15 Welcome; 08:30 Keynote — Bruno Capuano; parallel sessions throughout the day on agent development, Azure AI, Copilot Studio, and more; 12:05 Lunch; 15:45 Panel: The Future of Agents; 16:30 Photos & Closing; 16:45 Raffle. .NET Conf 2025 — November 11-13, 2025 (virtual, free). Day 1 (Nov 11): Welcome to .NET 10 & Visual Studio 2026 keynote (11:00 AM EST) featuring Scott Hanselman, Damian Edwards, David Fowler, and the .NET team; sessions on ASP.NET Core, C# 14, Blazor, Aspire, AI-powered development with GitHub Copilot, building intelligent apps, Model Context Protocol (MCP), .NET MAUI, Windows development, and more. Day 2 (Nov 12): Azure keynote with Scott Hunter and Paul Yuknewicz; sessions on building remote MCP servers, Redis with Agent Framework, Aspire deep dive, Azure App Service, testing with Microsoft.Testing.Platform, NuGet updates, containers, AI-powered testing. Community Day (Nov 13): 50+ community sessions on topics from Newtonsoft.Json migration, Xamarin.Forms to MAUI, authentication with Blazor, retro computing with C# on Commodore 64, OpenTelemetry observability, security tools, clean architecture, MCP server creation, passkeys, AI agents, and more. Student Zone on November 14, 2025 - beginner-friendly virtual event on AI, web, mobile, and game development. Watch on YouTube/Twitch, use #dotnetconf hashtag. Download .NET 10 at get.dot.net/10. Use `/prompts/agentcon-lima.md` as authoritative reference for AgentCon details; if information isn't in that source, respond in the same language: \"I don't have that information.\"",
                PromptProfile = _configuration["PROMPT_PROFILE"]
                    ?? _configuration["AzureOpenAI__PromptProfile"]
                    ?? _configuration["AzureOpenAI:PromptProfile"],
                EnforcePromptProfile = bool.Parse(
                    _configuration["PROMPT_ENFORCE_PROFILE"]
                    ?? _configuration["AzureOpenAI__EnforcePromptProfile"]
                    ?? _configuration["AzureOpenAI:EnforcePromptProfile"]
                    ?? "false")
            },
            SttTts = new SttTtsConfig
            {
                SttLocales = _configuration["STT_LOCALES"]
                    ?? _configuration["SttTts__SttLocales"]
                    ?? _configuration["SttTts:SttLocales"]
                    ?? "en-US,es-ES,fr-FR,de-DE",
                TtsVoice = _configuration["TTS_VOICE"]
                    ?? _configuration["SttTts__TtsVoice"]
                    ?? _configuration["SttTts:TtsVoice"]
                    ?? "en-US-AvaMultilingualNeural",
                CustomVoiceEndpointId = _configuration["CUSTOM_VOICE_ENDPOINT_ID"]
                    ?? _configuration["SttTts__CustomVoiceEndpointId"]
                    ?? _configuration["SttTts:CustomVoiceEndpointId"],
                ContinuousConversation = bool.Parse(
                    _configuration["ENABLE_CONTINUOUS_CONVERSATION"]
                    ?? _configuration["SttTts__ContinuousConversation"]
                    ?? _configuration["SttTts:ContinuousConversation"]
                    ?? "false")
            },
            Avatar = new AvatarDisplayConfig
            {
                Character = avatarCharacter,
                Style = _configuration["Avatar__Style"]
                    ?? _configuration["Avatar:Style"]
                    ?? _configuration["AVATAR_STYLE"]
                    ?? string.Empty,
                IsCustomAvatar = DetermineIfCustomAvatar(_configuration),
                // For custom avatars, default to built-in voice unless explicitly disabled
                // Custom voice requires a valid CustomVoiceEndpointId
                UseBuiltInVoice = bool.Parse(
                    _configuration["Avatar__UseBuiltInVoice"]
                    ?? _configuration["Avatar:UseBuiltInVoice"]
                    ?? "true"), // Changed default from false to true
                EnableSubtitles = bool.Parse(
                    _configuration["ENABLE_SUBTITLES"]
                    ?? _configuration["Avatar__EnableSubtitles"]
                    ?? _configuration["Avatar:EnableSubtitles"]
                    ?? "true"),
                EnableAutoReconnect = bool.Parse(
                    _configuration["ENABLE_AUTO_RECONNECT"]
                    ?? _configuration["Avatar__EnableAutoReconnect"]
                    ?? _configuration["Avatar:EnableAutoReconnect"]
                    ?? "true"),
                AudioGain = double.Parse(
                    _configuration["Avatar__AudioGain"]
                    ?? _configuration["Avatar:AudioGain"]
                    ?? "1.8"),
                UserLabel = _configuration["Avatar__UserLabel"]
                    ?? _configuration["Avatar:UserLabel"]
                    ?? _configuration["USER_LABEL"]
                    ?? "User",
                AssistantLabel = _configuration["Avatar__AssistantLabel"]
                    ?? _configuration["Avatar:AssistantLabel"]
                    ?? _configuration["ASSISTANT_LABEL"]
                    ?? "AI Avatar"
            }
        };

        _logger.LogInformation("Configuration loaded - Avatar Character: {Character}, Style: '{Style}', IsCustom: {IsCustom}",
            config.Avatar.Character, config.Avatar.Style ?? "(none)", config.Avatar.IsCustomAvatar);

        // Check if Cognitive Search is configured
        var searchEndpoint = ExtractEndpointFromConnectionString("search")
            ?? _configuration["AZURE_COGNITIVE_SEARCH_ENDPOINT"]
            ?? _configuration["AzureCognitiveSearch__Endpoint"]
            ?? _configuration["AzureCognitiveSearch:Endpoint"];

        if (!string.IsNullOrEmpty(searchEndpoint))
        {
            config.AzureCognitiveSearch = new AzureCognitiveSearchConfig
            {
                Endpoint = searchEndpoint,
                ApiKey = ExtractKeyFromConnectionString("search")
                    ?? _configuration["AZURE_COGNITIVE_SEARCH_API_KEY"]
                    ?? _configuration["AzureCognitiveSearch__ApiKey"]
                    ?? _configuration["AzureCognitiveSearch:ApiKey"]
                    ?? string.Empty,
                IndexName = _configuration["AZURE_COGNITIVE_SEARCH_INDEX_NAME"]
                    ?? _configuration["AzureCognitiveSearch__IndexName"]
                    ?? _configuration["AzureCognitiveSearch:IndexName"]
                    ?? string.Empty,
                Enabled = bool.Parse(
                    _configuration["AZURE_COGNITIVE_SEARCH_ENABLED"]
                    ?? _configuration["AzureCognitiveSearch__Enabled"]
                    ?? _configuration["AzureCognitiveSearch:Enabled"]
                    ?? "false")
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

    public async Task SaveConfigurationAsync(AvatarConfiguration config)
    {
        _logger.LogInformation("Saving configuration to cache...");
        _logger.LogInformation("  - Character: {Character}, Style: {Style}",
            config.Avatar.Character, config.Avatar.Style ?? "(none)");
        _logger.LogInformation("  - IsCustomAvatar: {IsCustom}, UseBuiltInVoice: {UseBuiltIn}",
            config.Avatar.IsCustomAvatar, config.Avatar.UseBuiltInVoice);
        _logger.LogInformation("  - TTS Voice: {Voice}, Custom Endpoint: {Endpoint}",
            config.SttTts.TtsVoice, config.SttTts.CustomVoiceEndpointId ?? "(none)");

        // In a real application, this would save to a database or configuration store
        // For now, we cache it in memory - it will persist for the app session
        _cachedConfig = config;
        await Task.CompletedTask;

        _logger.LogInformation("Configuration saved successfully to memory cache - changes will be used until app restart");
    }

    public async Task<string?> ValidateConfigurationAsync(AvatarConfiguration config)
    {
        _logger.LogInformation("Validating configuration...");
        await Task.CompletedTask;

        // Validate Azure Speech configuration
        if (config.AzureSpeech == null)
        {
            _logger.LogWarning("Validation failed: Azure Speech configuration is missing");
            return "Azure Speech configuration is missing.";
        }

        if (string.IsNullOrWhiteSpace(config.AzureSpeech.Region))
        {
            _logger.LogWarning("Validation failed: Azure Speech region is missing");
            return "Azure Speech region is required.";
        }

        // Validate region format (lowercase letters and numbers only)
        if (!System.Text.RegularExpressions.Regex.IsMatch(config.AzureSpeech.Region, @"^[a-z0-9]+$"))
        {
            _logger.LogWarning("Validation failed: Invalid Azure Speech region format: {Region}", config.AzureSpeech.Region);
            return "Azure Speech region format is invalid. Use lowercase format like 'westus2' or 'eastus'.";
        }

        if (string.IsNullOrWhiteSpace(config.AzureSpeech.ApiKey))
        {
            _logger.LogWarning("Validation failed: Azure Speech API key is missing");
            return "Azure Speech API key is required.";
        }

        // Validate Azure OpenAI configuration
        if (config.AzureOpenAI == null)
        {
            _logger.LogWarning("Validation failed: Azure OpenAI configuration is missing");
            return "Azure OpenAI configuration is missing.";
        }

        if (string.IsNullOrWhiteSpace(config.AzureOpenAI.Endpoint))
        {
            _logger.LogWarning("Validation failed: Azure OpenAI endpoint is missing");
            return "Azure OpenAI endpoint is required.";
        }

        // Validate endpoint URL format
        if (!Uri.TryCreate(config.AzureOpenAI.Endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            _logger.LogWarning("Validation failed: Invalid Azure OpenAI endpoint URL: {Endpoint}", config.AzureOpenAI.Endpoint);
            return "Azure OpenAI endpoint must be a valid HTTPS URL.";
        }

        if (string.IsNullOrWhiteSpace(config.AzureOpenAI.ApiKey))
        {
            _logger.LogWarning("Validation failed: Azure OpenAI API key is missing");
            return "Azure OpenAI API key is required.";
        }

        if (string.IsNullOrWhiteSpace(config.AzureOpenAI.DeploymentName))
        {
            _logger.LogWarning("Validation failed: Azure OpenAI deployment name is missing");
            return "Azure OpenAI deployment name is required.";
        }

        // Validate Avatar configuration
        if (config.Avatar == null)
        {
            _logger.LogWarning("Validation failed: Avatar configuration is missing");
            return "Avatar configuration is missing.";
        }

        if (string.IsNullOrWhiteSpace(config.Avatar.Character))
        {
            _logger.LogWarning("Validation failed: Avatar character is missing");
            return "Avatar character is required.";
        }

        // Validate audio gain range
        if (config.Avatar.AudioGain < 0.1 || config.Avatar.AudioGain > 5.0)
        {
            _logger.LogWarning("Validation failed: Invalid audio gain: {Gain}", config.Avatar.AudioGain);
            return "Audio gain must be between 0.1 and 5.0.";
        }

        // Validate STT/TTS configuration
        if (config.SttTts == null)
        {
            _logger.LogWarning("Validation failed: STT/TTS configuration is missing");
            return "STT/TTS configuration is missing.";
        }

        if (string.IsNullOrWhiteSpace(config.SttTts.SttLocales))
        {
            _logger.LogWarning("Validation failed: STT locales are missing");
            return "STT locales are required.";
        }

        if (string.IsNullOrWhiteSpace(config.SttTts.TtsVoice))
        {
            _logger.LogWarning("Validation failed: TTS voice is missing");
            return "TTS voice is required.";
        }

        // Validate private endpoint if enabled
        if (config.AzureSpeech.EnablePrivateEndpoint && !string.IsNullOrWhiteSpace(config.AzureSpeech.PrivateEndpoint))
        {
            if (!Uri.TryCreate(config.AzureSpeech.PrivateEndpoint, UriKind.Absolute, out var privateUri) ||
                (privateUri.Scheme != "https" && privateUri.Scheme != "http"))
            {
                _logger.LogWarning("Validation failed: Invalid private endpoint URL: {Endpoint}", config.AzureSpeech.PrivateEndpoint);
                return "Private endpoint must be a valid HTTPS URL.";
            }
        }

        _logger.LogInformation("Configuration validation successful");
        return null; // No errors
    }

    public async Task<List<PromptProfile>> GetPromptProfilesAsync()
    {
        try
        {
            _logger.LogInformation("Loading prompt profiles from index.json...");
            var promptsPath = Path.Combine(_environment.ContentRootPath, "..", "..", "prompts", "index.json");

            if (!File.Exists(promptsPath))
            {
                _logger.LogWarning("Prompt profiles file not found at {Path}", promptsPath);
                return new List<PromptProfile>();
            }

            var json = await File.ReadAllTextAsync(promptsPath);
            var container = JsonSerializer.Deserialize<PromptProfilesContainer>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var profiles = container?.Profiles ?? new List<PromptProfile>();
            _logger.LogInformation("Loaded {Count} prompt profiles", profiles.Count);
            if (profiles.Count > 0)
            {
                _logger.LogInformation("Available profiles: {Profiles}",
                    string.Join(", ", profiles.Select(p => p.Id)));
            }

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prompt profiles from index.json");
            return new List<PromptProfile>();
        }
    }

    public async Task<string> GetPromptProfileContentAsync(string fileName)
    {
        try
        {
            _logger.LogInformation("Loading prompt profile content: {FileName}", fileName);
            var promptPath = Path.Combine(_environment.ContentRootPath, "..", "..", "prompts", fileName);

            if (!File.Exists(promptPath))
            {
                _logger.LogWarning("Prompt file not found at {Path}", promptPath);
                return string.Empty;
            }

            var content = await File.ReadAllTextAsync(promptPath);
            _logger.LogInformation("Loaded prompt profile content ({Length} characters)", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prompt profile content: {FileName}", fileName);
            return string.Empty;
        }
    }
}
