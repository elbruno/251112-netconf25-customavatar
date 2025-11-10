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

    public AvatarConfiguration GetConfiguration()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        var config = new AvatarConfiguration
        {
            AzureSpeech = new AzureSpeechConfig
            {
                Region = _configuration["AzureSpeech:Region"] ?? "westus2",
                ApiKey = _configuration["AzureSpeech:ApiKey"] ?? string.Empty,
                EnablePrivateEndpoint = bool.Parse(_configuration["AzureSpeech:EnablePrivateEndpoint"] ?? "false"),
                PrivateEndpoint = _configuration["AzureSpeech:PrivateEndpoint"]
            },
            AzureOpenAI = new AzureOpenAIConfig
            {
                Endpoint = _configuration["AzureOpenAI:Endpoint"] ?? string.Empty,
                ApiKey = _configuration["AzureOpenAI:ApiKey"] ?? string.Empty,
                DeploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? string.Empty,
                SystemPrompt = _configuration["AzureOpenAI:SystemPrompt"] ?? "You are an AI assistant that helps people find information.",
                PromptProfile = _configuration["AzureOpenAI:PromptProfile"],
                EnforcePromptProfile = bool.Parse(_configuration["AzureOpenAI:EnforcePromptProfile"] ?? "false")
            },
            SttTts = new SttTtsConfig
            {
                SttLocales = _configuration["SttTts:SttLocales"] ?? "en-US,es-ES,fr-FR,de-DE",
                TtsVoice = _configuration["SttTts:TtsVoice"] ?? "en-US-AvaMultilingualNeural",
                CustomVoiceEndpointId = _configuration["SttTts:CustomVoiceEndpointId"],
                ContinuousConversation = bool.Parse(_configuration["SttTts:ContinuousConversation"] ?? "false")
            },
            Avatar = new AvatarDisplayConfig
            {
                Character = _configuration["Avatar:Character"] ?? "lisa",
                Style = _configuration["Avatar:Style"] ?? "casual-sitting",
                IsCustomAvatar = bool.Parse(_configuration["Avatar:IsCustomAvatar"] ?? "false"),
                UseBuiltInVoice = bool.Parse(_configuration["Avatar:UseBuiltInVoice"] ?? "false"),
                EnableSubtitles = bool.Parse(_configuration["Avatar:EnableSubtitles"] ?? "false"),
                EnableAutoReconnect = bool.Parse(_configuration["Avatar:EnableAutoReconnect"] ?? "false"),
                AudioGain = double.Parse(_configuration["Avatar:AudioGain"] ?? "1.8")
            }
        };

        // Check if Cognitive Search is configured
        var searchEndpoint = _configuration["AzureCognitiveSearch:Endpoint"];
        if (!string.IsNullOrEmpty(searchEndpoint))
        {
            config.AzureCognitiveSearch = new AzureCognitiveSearchConfig
            {
                Endpoint = searchEndpoint,
                ApiKey = _configuration["AzureCognitiveSearch:ApiKey"] ?? string.Empty,
                IndexName = _configuration["AzureCognitiveSearch:IndexName"] ?? string.Empty,
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
