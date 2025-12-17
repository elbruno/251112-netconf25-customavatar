namespace AzureAIAvatarBlazor.Models;

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
