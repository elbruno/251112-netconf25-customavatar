namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Configuration model for Azure AI Avatar application
/// </summary>
public class AvatarConfiguration
{
    /// <summary>
    /// Azure Speech Service configuration
    /// </summary>
    public AzureSpeechConfig AzureSpeech { get; set; } = new();

    /// <summary>
    /// Azure OpenAI configuration
    /// </summary>
    public AzureOpenAIConfig AzureOpenAI { get; set; } = new();

    /// <summary>
    /// Azure Cognitive Search configuration (optional, for On Your Data)
    /// </summary>
    public AzureCognitiveSearchConfig? AzureCognitiveSearch { get; set; }

    /// <summary>
    /// STT/TTS configuration
    /// </summary>
    public SttTtsConfig SttTts { get; set; } = new();

    /// <summary>
    /// Avatar display configuration
    /// </summary>
    public AvatarDisplayConfig Avatar { get; set; } = new();
}

/// <summary>
/// Azure Speech Service configuration
/// </summary>
public class AzureSpeechConfig
{
    public string Region { get; set; } = "westus2";
    public string ApiKey { get; set; } = string.Empty;
    public bool EnablePrivateEndpoint { get; set; }
    public string? PrivateEndpoint { get; set; }
}

/// <summary>
/// Azure OpenAI configuration
/// </summary>
public class AzureOpenAIConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "You are an AI assistant that helps people find information.";
    public string? PromptProfile { get; set; }
    public bool EnforcePromptProfile { get; set; }
    public Dictionary<string, string> PromptVariables { get; set; } = new();
    
    /// <summary>
    /// Configuration mode: LLM, Agent-LLM, or Agent-AIFoundry
    /// </summary>
    public string Mode { get; set; } = "LLM";
    
    /// <summary>
    /// Azure AI Foundry Agent ID (required when Mode is Agent-AIFoundry)
    /// </summary>
    public string? AgentId { get; set; }
}

/// <summary>
/// Azure Cognitive Search configuration
/// </summary>
public class AzureCognitiveSearchConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

/// <summary>
/// STT/TTS configuration
/// </summary>
public class SttTtsConfig
{
    public string SttLocales { get; set; } = "";
    public string TtsVoice { get; set; } = "";
    public string? CustomVoiceEndpointId { get; set; }
    public bool ContinuousConversation { get; set; }
}

/// <summary>
/// Avatar display configuration
/// </summary>
public class AvatarDisplayConfig
{
    public string Character { get; set; } = "lisa";
    public string Style { get; set; } = string.Empty;
    public bool IsCustomAvatar { get; set; }
    public bool UseBuiltInVoice { get; set; }
    public bool EnableSubtitles { get; set; }
    public bool EnableAutoReconnect { get; set; }
    public double AudioGain { get; set; } = 1.8;
    public string UserLabel { get; set; } = "User";
    public string AssistantLabel { get; set; } = "AI Avatar";
}
