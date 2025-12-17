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
    public AzureOpenAI AzureOpenAI { get; set; } = new();

    /// <summary>
    /// STT/TTS configuration
    /// </summary>
    public SttTtsConfig SttTts { get; set; } = new();

    /// <summary>
    /// Avatar display configuration
    /// </summary>
    public AvatarDisplayConfig Avatar { get; set; } = new();

    /// <summary>
    /// Optional set of predefined questions (1-5) available in the chat UI
    /// </summary>
    public List<PredefinedQuestion> PredefinedQuestions { get; set; } = new();
}
