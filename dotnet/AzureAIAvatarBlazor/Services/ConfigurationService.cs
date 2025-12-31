using AzureAIAvatarBlazor.Models;
using AzureAIAvatarBlazor.Services.Caching;
using System.Text.Json;

namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Configuration service that manages user-configurable settings.
/// 
/// IMPORTANT: AppHost-managed secrets (Application Insights, Microsoft Foundry endpoint, TenantId)
/// are NOT directly editable in the UI. These are managed through Aspire AppHost connection strings.
/// 
/// The application now uses Microsoft Foundry (via AzureAIAvatarBlazor.MAFFoundry library) for AI operations.
/// IChatClient is automatically registered when the Microsoft Foundry endpoint is configured.
/// 
/// Phase 3: Configuration is now cached in Redis to reduce file/environment variable reads.
/// </summary>
public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly TelemetryService _telemetryService;
    private readonly ICachingService _cachingService;
    private AvatarConfiguration? _cachedConfig;
    private const string ConfigCacheKey = "config:default";
    private static readonly TimeSpan ConfigCacheExpiration = TimeSpan.FromMinutes(5);

    public ConfigurationService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<ConfigurationService> logger,
        TelemetryService telemetryService,
        ICachingService cachingService)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _telemetryService = telemetryService;
        _cachingService = cachingService;
    }

    public event EventHandler<AvatarConfiguration?>? ConfigurationChanged;
    private bool DetermineIfCustomAvatar(IConfiguration config)
    {
        // Check if explicitly set
        var explicitSetting = config["Avatar__IsCustomAvatar"]
            ?? config["Avatar:IsCustomAvatar"];

        if (!string.IsNullOrEmpty(explicitSetting))
        {
            var result = bool.Parse(explicitSetting);
            _logger.LogInformation("Using explicit custom avatar setting: {IsCustomAvatar}, Source: Configuration", result);
            return result;
        }

        // Auto-detect using the centralized method
        var character = config["Avatar__Character"]
            ?? config["Avatar:Character"]
            ?? config["AVATAR_CHARACTER"]
            ?? "lisa";

        var isCustom = AvatarDisplayConfig.IsCustomAvatarCharacter(character);
        _logger.LogInformation("Auto-detected custom avatar: {IsCustomAvatar}, Character: {AvatarCharacter}", isCustom, character);

        return isCustom;
    }

    public AvatarConfiguration GetConfiguration()
    {
        using var activity = _telemetryService.StartConfigLoadSpan("cache-check");
        
        // First, check in-memory cache (respects user changes from Config page)
        if (_cachedConfig != null)
        {
            activity?.SetTag("config.cache_hit", "memory");
            _logger.LogDebug("Returning in-memory cached configuration (Character: {AvatarCharacter}, IsCustom: {IsCustomAvatar}, UseBuiltInVoice: {UseBuiltInVoice})",
                _cachedConfig.Avatar.Character,
                _cachedConfig.Avatar.IsCustomAvatar,
                _cachedConfig.Avatar.UseBuiltInVoice);
            return _cachedConfig;
        }

        // Second, check Redis cache
        try
        {
            var cachedFromRedis = _cachingService.GetAsync<AvatarConfiguration>(ConfigCacheKey).GetAwaiter().GetResult();
            if (cachedFromRedis != null)
            {
                activity?.SetTag("config.cache_hit", "redis");
                _logger.LogInformation("Returning Redis cached configuration (Character: {Character})", cachedFromRedis.Avatar.Character);
                _cachedConfig = cachedFromRedis; // Also cache in memory
                return cachedFromRedis;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get configuration from Redis cache, loading from environment");
        }

        activity?.SetTag("config.cache_hit", "miss");
        activity?.SetTag("config.source", "environment");
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
                ApiKey = _configuration["AZURE_SPEECH_API_KEY"]
                    ?? _configuration["AzureSpeech__ApiKey"]
                    ?? _configuration["AzureSpeech:ApiKey"]
                    ?? string.Empty,
                // DO NOT use PrivateEndpoint for avatars - always use standard regional endpoint
                PrivateEndpoint = _configuration["AzureSpeech:PrivateEndpoint"]
                ?? "",
                // Force EnablePrivateEndpoint to false for avatar support
                EnablePrivateEndpoint = bool.TryParse(
                    _configuration["AzureSpeech:EnablePrivateEndpoint"], out var enablePrivateEndpointValue)
                    ? enablePrivateEndpointValue
                    : false,
            },
            AzureOpenAI = new AzureOpenAIConfig
            {
                // Mode is now fixed to Agent-MicrosoftFoundry (managed by MAFFoundry library)
                // Microsoft Foundry endpoint is managed by Aspire AppHost via connection strings
                Mode = "Agent-MicrosoftFoundry",
                TenantId = _configuration.GetConnectionString("tenantId")
                    ?? _configuration["AZURE_TENANT_ID"]
                    ?? _configuration["AzureOpenAI__TenantId"]
                    ?? _configuration["AzureOpenAI:TenantId"]
                    ?? string.Empty,
                AgentLLM = new AgentLLMConfig
                {
                    // Legacy fields - endpoint and API key are now managed by AppHost
                    Endpoint = string.Empty, // Managed by MAFFoundry
                    ApiKey = string.Empty, // Managed by MAFFoundry
                    DeploymentName = _configuration["OpenAI__DeploymentName"]
                        ?? _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
                        ?? _configuration["AzureOpenAI__DeploymentName"]
                        ?? _configuration["AzureOpenAI:AgentLLMConfig:DeploymentName"]
                        ?? "gpt-4o-mini",
                    SystemPrompt = _configuration["SystemPrompt"]
                        ?? _configuration["AzureOpenAI__AgentLLM__SystemPrompt"]
                        ?? _configuration["AzureOpenAI:AgentLLM:SystemPrompt"]
                        ?? _configuration["AzureOpenAI__SystemPrompt"]
                        ?? _configuration["AzureOpenAI:AgentLLMConfig:SystemPrompt"]
                        ?? _configuration["SYSTEM_PROMPT"]
                        ?? "You are a helpful AI assistant. Respond in the user's language with brief answers (1-2 sentences) and a friendly, approachable tone.",
                },
                AgentAIFoundry = new AgentAIFoundryConfig
                {
                    // Not used - kept for backward compatibility
                    AgentId = string.Empty,
                    AIFoundryEndpoint = string.Empty
                },
                AgentMicrosoftFoundry = new AgentMicrosoftFoundryConfig
                {
                    // Microsoft Foundry endpoint is managed by Aspire AppHost
                    MicrosoftFoundryEndpoint = _configuration.GetConnectionString("microsoftfoundryproject") ?? string.Empty,
                    MicrosoftFoundryAgentName = _configuration["AI_AgentName"]
                        ?? _configuration["AgentName"]
                        ?? _configuration["AzureOpenAI:AgentMicrosoftFoundry:AgentName"]
                        ?? "AvatarAgent"
                }
            },
            SttTts = new SttTtsConfig
            {
                SttLocales = _configuration["STT_LOCALES"]
                    ?? _configuration["SttTts__SttLocales"]
                    ?? _configuration["SttTts:SttLocales"]
                    ?? "en-US",
                TtsVoice = _configuration["TTS_VOICE"]
                    ?? _configuration["SttTts__TtsVoice"]
                    ?? _configuration["SttTts:TtsVoice"]
                    ?? "",
                CustomVoiceEndpointId = _configuration["CUSTOM_VOICE_ENDPOINT_ID"]
                    ?? _configuration["SttTts__CustomVoiceEndpointId"]
                    ?? _configuration["SttTts:CustomVoiceEndpointId"]
                    ?? "",
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

        // Load predefined questions from configuration (supports appsettings JSON array or env vars like PredefinedQuestions:0:Title)
        try
        {
            var predefinedSection = _configuration.GetSection("PredefinedQuestions");
            if (predefinedSection.Exists())
            {
                var list = predefinedSection.Get<List<PredefinedQuestion>>() ?? new List<PredefinedQuestion>();
                if (list.Count > 0)
                {
                    // Limit to maximum 5 questions
                    config.PredefinedQuestions = list.Take(5).Where(p => !string.IsNullOrWhiteSpace(p.Question)).ToList();
                    _logger.LogInformation("Loaded {Count} predefined questions from configuration", config.PredefinedQuestions.Count);
                }
            }
            else
            {
                // Also support a single environment variable containing JSON array
                var envJson = _configuration["PREDEFINED_QUESTIONS_JSON"];
                if (!string.IsNullOrWhiteSpace(envJson))
                {
                    try
                    {
                        var parsed = System.Text.Json.JsonSerializer.Deserialize<List<PredefinedQuestion>>(envJson, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<PredefinedQuestion>();
                        config.PredefinedQuestions = parsed.Take(5).Where(p => !string.IsNullOrWhiteSpace(p.Question)).ToList();
                        _logger.LogInformation("Loaded {Count} predefined questions from PREDEFINED_QUESTIONS_JSON env var", config.PredefinedQuestions.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse PREDEFINED_QUESTIONS_JSON environment variable");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading predefined questions from configuration");
        }

        // Load Avatars collection from configuration if available
        try
        {
            var avatarsSection = _configuration.GetSection("Avatars");
            if (avatarsSection.Exists())
            {
                var list = avatarsSection.Get<List<AvatarProfile>>() ?? new List<AvatarProfile>();
                if (list.Count > 0)
                {
                    config.Avatars = list;
                }
            }

            // SelectedAvatarId handling
            var selectedId = _configuration["SelectedAvatarId"] ?? _configuration["Avatar__SelectedAvatarId"] ?? _configuration["SelectedAvatarId"];
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                config.SelectedAvatarId = selectedId;
            }

            // If a SelectedAvatarId is present, set active Avatar accordingly
            if (!string.IsNullOrWhiteSpace(config.SelectedAvatarId) && config.Avatars != null && config.Avatars.Count > 0)
            {
                var match = config.Avatars.FirstOrDefault(a => string.Equals(a.Id, config.SelectedAvatarId, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    config.Avatar.Character = match.Character;
                    config.Avatar.Style = match.Style;
                    config.Avatar.IsCustomAvatar = match.IsCustomAvatar;
                    config.Avatar.UseBuiltInVoice = match.UseBuiltInVoice;
                    config.Avatar.EnableSubtitles = match.EnableSubtitles;
                    config.Avatar.EnableAutoReconnect = match.EnableAutoReconnect;
                    config.Avatar.AudioGain = match.AudioGain;
                    config.Avatar.UserLabel = match.UserLabel;
                    config.Avatar.AssistantLabel = match.AssistantLabel;

                    // Apply per-avatar TTS settings to avatar display config
                    config.Avatar.TtsVoice = match.TtsVoice ?? config.Avatar.TtsVoice;
                    config.Avatar.CustomVoiceEndpointId = match.CustomVoiceEndpointId ?? config.Avatar.CustomVoiceEndpointId;

                    // Also prefer avatar-level TTS settings for global SttTts if they are present
                    if (!string.IsNullOrWhiteSpace(match.TtsVoice)) config.SttTts.TtsVoice = match.TtsVoice;
                    if (!string.IsNullOrWhiteSpace(match.CustomVoiceEndpointId)) config.SttTts.CustomVoiceEndpointId = match.CustomVoiceEndpointId;
                }
            }
            else
            {
                // Fallback: if no SelectedAvatarId but Avatars list exists, use the first one
                if (config.Avatars != null && config.Avatars.Count > 0)
                {
                    var first = config.Avatars[0];
                    config.SelectedAvatarId = first.Id;
                    config.Avatar.Character = first.Character;
                    config.Avatar.Style = first.Style;
                    config.Avatar.IsCustomAvatar = first.IsCustomAvatar;
                    config.Avatar.UseBuiltInVoice = first.UseBuiltInVoice;
                    config.Avatar.EnableSubtitles = first.EnableSubtitles;
                    config.Avatar.EnableAutoReconnect = first.EnableAutoReconnect;
                    config.Avatar.AudioGain = first.AudioGain;
                    config.Avatar.UserLabel = first.UserLabel;
                    config.Avatar.AssistantLabel = first.AssistantLabel;

                    // Apply per-avatar TTS settings from first avatar
                    config.Avatar.TtsVoice = first.TtsVoice ?? config.Avatar.TtsVoice;
                    config.Avatar.CustomVoiceEndpointId = first.CustomVoiceEndpointId ?? config.Avatar.CustomVoiceEndpointId;

                    if (!string.IsNullOrWhiteSpace(first.TtsVoice)) config.SttTts.TtsVoice = first.TtsVoice;
                    if (!string.IsNullOrWhiteSpace(first.CustomVoiceEndpointId)) config.SttTts.CustomVoiceEndpointId = first.CustomVoiceEndpointId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Avatars collection from configuration");
        }

        _cachedConfig = config;
        
        // Cache in Redis for multi-instance scenarios (sync call in async wrapper)
        try
        {
            _cachingService.SetAsync(ConfigCacheKey, config, ConfigCacheExpiration).GetAwaiter().GetResult();
            _logger.LogInformation("Configuration cached in Redis with {Expiration} expiration", ConfigCacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache configuration in Redis");
        }
        
        return config;
    }

    public async Task SaveConfigurationAsync(AvatarConfiguration config)
    {
        // Count changed keys for tracing
        var changedKeys = new List<string>();
        if (_cachedConfig != null)
        {
            if (_cachedConfig.Avatar.Character != config.Avatar.Character) changedKeys.Add("Avatar.Character");
            if (_cachedConfig.AzureOpenAI.Mode != config.AzureOpenAI.Mode) changedKeys.Add("AzureOpenAI.Mode");
            if (_cachedConfig.Avatar.IsCustomAvatar != config.Avatar.IsCustomAvatar) changedKeys.Add("Avatar.IsCustomAvatar");
            if (_cachedConfig.Avatar.UseBuiltInVoice != config.Avatar.UseBuiltInVoice) changedKeys.Add("Avatar.UseBuiltInVoice");
        }

        using var activity = _telemetryService.StartConfigSaveSpan(changedKeys.Count);
        activity?.SetTag("config.changed_keys", string.Join(", ", changedKeys));
        
        _logger.LogInformation("Saving configuration to cache...");
        _logger.LogInformation("  - Character: {Character}, Style: {Style}",
            config.Avatar.Character, config.Avatar.Style ?? "(none)");
        _logger.LogInformation("  - IsCustomAvatar: {IsCustom}, UseBuiltInVoice: {UseBuiltIn}",
            config.Avatar.IsCustomAvatar, config.Avatar.UseBuiltInVoice);
        _logger.LogInformation("  - TTS Voice: {Voice}, Custom Endpoint: {Endpoint}",
            config.SttTts.TtsVoice, config.SttTts.CustomVoiceEndpointId ?? "(none)");

        if (config.PredefinedQuestions != null && config.PredefinedQuestions.Count > 0)
        {
            _logger.LogInformation("  - PredefinedQuestions: {Count}", config.PredefinedQuestions.Count);
        }
        
        // Cache in Redis
        try
        {
            await _cachingService.SetAsync(ConfigCacheKey, config, ConfigCacheExpiration);
            activity?.SetTag("config.redis_save", "success");
            _logger.LogInformation("Configuration saved to Redis cache");
        }
        catch (Exception ex)
        {
            activity?.SetTag("config.redis_save", "failed");
            activity?.SetTag("config.error", ex.Message);
            _logger.LogWarning(ex, "Failed to save configuration to Redis cache");
        }
        
        // Track configuration changes
        var oldCharacter = _cachedConfig?.Avatar.Character;
        var newCharacter = config.Avatar.Character;
        if (oldCharacter != newCharacter)
        {
            _telemetryService.TrackConfigurationChange("Avatar.Character", oldCharacter, newCharacter);
        }
        
        var oldMode = _cachedConfig?.AzureOpenAI.Mode;
        var newMode = config.AzureOpenAI.Mode;
        if (oldMode != newMode)
        {
            _telemetryService.TrackConfigurationChange("AzureOpenAI.Mode", oldMode, newMode);
        }
        
        // In a real application, this would save to a database or configuration store
        // For now, we cache it in memory - it will persist for the app session
        _cachedConfig = config;
        await Task.CompletedTask;

        _logger.LogInformation("Configuration saved successfully to memory cache - changes will be used until app restart");

        // Notify subscribers about the change
        try
        {
            ConfigurationChanged?.Invoke(this, _cachedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while invoking ConfigurationChanged event");
        }
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

        var mode = config.AzureOpenAI.Mode ?? "Agent-LLM";

        // Validate based on mode
        if (mode == "Agent-LLM")
        {
            // Standard LLM validation
            if (string.IsNullOrWhiteSpace(config.AzureOpenAI.AgentLLM.Endpoint))
            {
                _logger.LogWarning("Validation failed: Azure OpenAI endpoint is missing");
                return "Azure OpenAI endpoint is required.";
            }

            // Validate endpoint URL format
            if (!Uri.TryCreate(config.AzureOpenAI.AgentLLM.Endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
            {
                _logger.LogWarning("Validation failed: Invalid Azure OpenAI endpoint URL: {Endpoint}", config.AzureOpenAI.AgentLLM.Endpoint);
                return "Azure OpenAI endpoint must be a valid HTTPS URL.";
            }

            if (string.IsNullOrWhiteSpace(config.AzureOpenAI.AgentLLM.ApiKey))
            {
                _logger.LogWarning("Validation failed: Azure OpenAI API key is missing");
                return "Azure OpenAI API key is required.";
            }

            if (string.IsNullOrWhiteSpace(config.AzureOpenAI.AgentLLM.DeploymentName))
            {
                _logger.LogWarning("Validation failed: Azure OpenAI deployment name is missing");
                return "Azure OpenAI deployment name is required.";
            }
        }
        else if (mode == "Agent-AIFoundry")
        {
            // Agent-AIFoundry mode validation
            if (string.IsNullOrWhiteSpace(config.AzureOpenAI.AgentAIFoundry.AIFoundryEndpoint))
            {
                _logger.LogWarning("Validation failed: Azure AI Foundry endpoint is required for Agent-AIFoundry mode");
                return "Azure AI Foundry endpoint is required for Agent-AIFoundry mode.";
            }

            if (!Uri.TryCreate(config.AzureOpenAI.AgentAIFoundry.AIFoundryEndpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
            {
                _logger.LogWarning("Validation failed: Invalid Azure AI Foundry endpoint URL: {Endpoint}", config.AzureOpenAI.AgentAIFoundry.AIFoundryEndpoint);
                return "Azure AI Foundry endpoint must be a valid HTTPS URL.";
            }

            if (string.IsNullOrWhiteSpace(config.AzureOpenAI.AgentAIFoundry.AgentId))
            {
                _logger.LogWarning("Validation failed: Agent ID is required for Agent-AIFoundry mode");
                return "Agent ID is required for Agent-AIFoundry mode.";
            }
        }
        else if (mode == "Agent-MicrosoftFoundry")
        {
            // Microsoft Foundry mode validation
            if (string.IsNullOrWhiteSpace(config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryEndpoint))
            {
                _logger.LogWarning("Validation failed: Microsoft Foundry endpoint is required for Agent-MicrosoftFoundry mode");
                return "Microsoft Foundry endpoint is required for Agent-MicrosoftFoundry mode.";
            }

            if (!Uri.TryCreate(config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryEndpoint, UriKind.Absolute, out var mfUri) || (mfUri.Scheme != "https" && mfUri.Scheme != "http"))
            {
                _logger.LogWarning("Validation failed: Invalid Microsoft Foundry endpoint URL: {Endpoint}", config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryEndpoint);
                return "Microsoft Foundry endpoint must be a valid HTTPS URL.";
            }

            if (string.IsNullOrWhiteSpace(config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryAgentName))
            {
                _logger.LogWarning("Validation failed: Microsoft Foundry agent name is required for Agent-MicrosoftFoundry mode");
                return "Microsoft Foundry agent name is required for Agent-MicrosoftFoundry mode.";
            }
        }
        else
        {
            _logger.LogWarning("Validation failed: Invalid mode: {Mode}", mode);
            return $"Invalid mode '{mode}'. Supported modes are: Agent-LLM, Agent-AIFoundry, Agent-MicrosoftFoundry.";
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

        // TTS voice is only required if NOT using built-in voice
        if (!config.Avatar.UseBuiltInVoice && string.IsNullOrWhiteSpace(config.SttTts.TtsVoice))
        {
            _logger.LogWarning("Validation failed: TTS voice is missing (and UseBuiltInVoice is false)");
            return "TTS voice is required when not using built-in avatar voice.";
        }

        _logger.LogInformation("TTS voice validation: UseBuiltInVoice={UseBuiltIn}, TtsVoice='{Voice}'",
            config.Avatar.UseBuiltInVoice,
            config.SttTts.TtsVoice ?? "(empty)");

        // If avatar requires a custom voice endpoint ensure it is present and not a placeholder
        if (!config.Avatar.UseBuiltInVoice)
        {
            var endpoint = config.SttTts?.CustomVoiceEndpointId ?? config.Avatar?.CustomVoiceEndpointId ?? string.Empty;
            var endpointTrim = (endpoint ?? string.Empty).Trim().ToLowerInvariant();
            var isPlaceholderEndpoint = string.IsNullOrWhiteSpace(endpointTrim) || endpointTrim == "your_custom_voice_endpoint_id" || endpointTrim.StartsWith("xxxxx") || endpointTrim.StartsWith("your-");
            if (isPlaceholderEndpoint)
            {
                _logger.LogWarning("Validation failed: CustomVoiceEndpointId is missing or placeholder: '{Endpoint}'", endpoint);
                return "CustomVoiceEndpointId is required and must be a valid endpoint id when not using built-in avatar voice.";
            }
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

        _logger.LogInformation("Configuration validation passed");
        return null;
    }
}
