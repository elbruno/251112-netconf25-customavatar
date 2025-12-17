namespace AzureAIAvatarBlazor.Models;

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

    // Per-avatar TTS settings (if present these override SttTts values)
    public string TtsVoice { get; set; } = string.Empty;
    public string? CustomVoiceEndpointId { get; set; }
}
