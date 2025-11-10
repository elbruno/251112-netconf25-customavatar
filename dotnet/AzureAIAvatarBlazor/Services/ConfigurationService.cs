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
        // Check if explicitly set
        var explicitSetting = config["AzureSpeech:EnablePrivateEndpoint"];
        if (!string.IsNullOrEmpty(explicitSetting))
        {
            return bool.Parse(explicitSetting);
        }

        // Auto-detect: if private endpoint URL is configured, enable it
        var privateEndpoint = config["AzureSpeech:PrivateEndpoint"] ?? config["AZURE_SPEECH_PRIVATE_ENDPOINT"];
        var shouldEnable = !string.IsNullOrEmpty(privateEndpoint);

        _logger.LogInformation("Private endpoint '{Endpoint}' - EnablePrivateEndpoint: {Enabled}",
            privateEndpoint ?? "null", shouldEnable);

        return shouldEnable;
    }

    private bool DetermineIfCustomAvatar(IConfiguration config)
    {
        // Check if explicitly set
        var explicitSetting = config["Avatar:IsCustomAvatar"];
        _logger.LogInformation("Explicit IsCustomAvatar setting: '{Setting}'", explicitSetting ?? "null");

        if (!string.IsNullOrEmpty(explicitSetting))
        {
            var result = bool.Parse(explicitSetting);
            _logger.LogInformation("Using explicit custom avatar setting: {Result}", result);
            return result;
        }

        // Auto-detect: custom avatars typically have "-Avatar-" in their name or aren't in the standard list
        var character = config["Avatar:Character"] ?? config["AVATAR_CHARACTER"] ?? "lisa";
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
        // Clear cache to ensure we always get fresh config with proper detection
        // if (_cachedConfig != null)
        //     return _cachedConfig;

        _logger.LogInformation("Loading configuration...");

        var avatarCharacter = _configuration["Avatar:Character"] ?? _configuration["AVATAR_CHARACTER"] ?? "lisa";
        _logger.LogInformation("Avatar character from config: '{Character}'", avatarCharacter);

        var config = new AvatarConfiguration
        {
            AzureSpeech = new AzureSpeechConfig
            {
                Region = _configuration["AzureSpeech:Region"] ?? _configuration["AZURE_SPEECH_REGION"] ?? "westus2",
                ApiKey = _configuration["AzureSpeech:ApiKey"] ?? _configuration["AZURE_SPEECH_API_KEY"] ?? string.Empty,
                PrivateEndpoint = _configuration["AzureSpeech:PrivateEndpoint"] ?? _configuration["AZURE_SPEECH_PRIVATE_ENDPOINT"],
                EnablePrivateEndpoint = DetermineEnablePrivateEndpoint(_configuration)
            },
            AzureOpenAI = new AzureOpenAIConfig
            {
                Endpoint = _configuration["AzureOpenAI:Endpoint"] ?? _configuration["AZURE_OPENAI_ENDPOINT"] ?? string.Empty,
                ApiKey = _configuration["AzureOpenAI:ApiKey"] ?? _configuration["AZURE_OPENAI_API_KEY"] ?? string.Empty,
                DeploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? string.Empty,
                SystemPrompt = _configuration["AzureOpenAI:SystemPrompt"] ?? _configuration["SYSTEM_PROMPT"] ?? "You are an AI assistant that helps people find information.",
                PromptProfile = _configuration["AzureOpenAI:PromptProfile"] ?? _configuration["PROMPT_PROFILE"],
                EnforcePromptProfile = bool.Parse(_configuration["AzureOpenAI:EnforcePromptProfile"] ?? _configuration["PROMPT_ENFORCE_PROFILE"] ?? "false")
            },
            SttTts = new SttTtsConfig
            {
                SttLocales = _configuration["SttTts:SttLocales"] ?? "en-US,es-ES,fr-FR,de-DE",
                TtsVoice = _configuration["SttTts:TtsVoice"] ?? _configuration["TTS_VOICE"] ?? "en-US-AvaMultilingualNeural",
                CustomVoiceEndpointId = _configuration["SttTts:CustomVoiceEndpointId"] ?? _configuration["CUSTOM_VOICE_ENDPOINT_ID"],
                ContinuousConversation = bool.Parse(_configuration["SttTts:ContinuousConversation"] ?? _configuration["ENABLE_CONTINUOUS_CONVERSATION"] ?? "false")
            },
            Avatar = new AvatarDisplayConfig
            {
                Character = avatarCharacter,
                Style = _configuration["Avatar:Style"] ?? _configuration["AVATAR_STYLE"] ?? string.Empty,
                IsCustomAvatar = DetermineIfCustomAvatar(_configuration),
                UseBuiltInVoice = bool.Parse(_configuration["Avatar:UseBuiltInVoice"] ?? "false"),
                EnableSubtitles = bool.Parse(_configuration["Avatar:EnableSubtitles"] ?? _configuration["ENABLE_SUBTITLES"] ?? "true"),
                EnableAutoReconnect = bool.Parse(_configuration["Avatar:EnableAutoReconnect"] ?? _configuration["ENABLE_AUTO_RECONNECT"] ?? "true"),
                AudioGain = double.Parse(_configuration["Avatar:AudioGain"] ?? "1.8")
            }
        };

        _logger.LogInformation("Configuration loaded - Avatar Character: {Character}, Style: '{Style}', IsCustom: {IsCustom}",
            config.Avatar.Character, config.Avatar.Style ?? "(none)", config.Avatar.IsCustomAvatar);

        // Check if Cognitive Search is configured
        var searchEndpoint = _configuration["AzureCognitiveSearch:Endpoint"] ?? _configuration["AZURE_COGNITIVE_SEARCH_ENDPOINT"];
        if (!string.IsNullOrEmpty(searchEndpoint))
        {
            config.AzureCognitiveSearch = new AzureCognitiveSearchConfig
            {
                Endpoint = searchEndpoint,
                ApiKey = _configuration["AzureCognitiveSearch:ApiKey"] ?? _configuration["AZURE_COGNITIVE_SEARCH_API_KEY"] ?? string.Empty,
                IndexName = _configuration["AzureCognitiveSearch:IndexName"] ?? _configuration["AZURE_COGNITIVE_SEARCH_INDEX_NAME"] ?? string.Empty,
                Enabled = bool.Parse(_configuration["AzureCognitiveSearch:Enabled"] ?? "false")
            };
        }

        _cachedConfig = config;
        return config;
    }

    public async Task SaveConfigurationAsync(AvatarConfiguration config)
    {
        // In a real application, this would save to a database or configuration store
        // For now, we just cache it in memory
        _cachedConfig = config;
        await Task.CompletedTask;
        _logger.LogInformation("Configuration saved to memory cache");
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
