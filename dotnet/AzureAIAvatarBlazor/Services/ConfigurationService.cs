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
            _logger.LogInformation("Returning cached configuration");
            return _cachedConfig;
        }

        _logger.LogInformation("Loading configuration from AppHost environment...");

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
                    ?? "You are an AI assistant that helps people find information.",
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
                    ?? "1.8")
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
        // In a real application, this would save to a database or configuration store
        // For now, we cache it in memory - it will persist for the app session
        _cachedConfig = config;
        await Task.CompletedTask;
        _logger.LogInformation("Configuration saved to memory cache - changes will be used until app restart");

        // Log key settings for debugging
        _logger.LogInformation("Saved config: Character={Character}, UseBuiltInVoice={UseBuiltIn}, EnablePrivateEndpoint={PrivateEndpoint}",
            config.Avatar.Character,
            config.Avatar.UseBuiltInVoice,
            config.AzureSpeech.EnablePrivateEndpoint);
    }

    public async Task<List<PromptProfile>> GetPromptProfilesAsync()
    {
        try
        {
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
            var promptPath = Path.Combine(_environment.ContentRootPath, "..", "..", "prompts", fileName);

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
