namespace AzureAIAvatarBlazor.Models;

public class AvatarProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description for the avatar profile (shown in UI)
    /// </summary>
    public string? Description { get; set; }
    
    public string Character { get; set; } = "lisa";
    public string Style { get; set; } = string.Empty;
    public bool IsCustomAvatar { get; set; }
    public bool UseBuiltInVoice { get; set; }
    public bool EnableSubtitles { get; set; }
    public bool EnableAutoReconnect { get; set; }
    public double AudioGain { get; set; } = 1.8;
    public string UserLabel { get; set; } = "User";
    public string AssistantLabel { get; set; } = "AI Avatar";

    // Per-avatar TTS settings
    public string TtsVoice { get; set; } = string.Empty;
    public string? CustomVoiceEndpointId { get; set; }

    /// <summary>
    /// Returns true if this is a standard (non-custom) Azure avatar
    /// </summary>
    public bool IsStandardAvatar => !IsCustomAvatar;

    /// <summary>
    /// Returns true if custom voice configuration is required but missing
    /// </summary>
    public bool HasMissingCustomVoiceConfig =>
        !UseBuiltInVoice && 
        (string.IsNullOrWhiteSpace(CustomVoiceEndpointId) || string.IsNullOrWhiteSpace(TtsVoice));

    /// <summary>
    /// Applies this profile's settings to the given avatar display config and optionally to SttTts config.
    /// </summary>
    public void ApplyTo(AvatarDisplayConfig avatarConfig, SttTtsConfig? sttTtsConfig = null)
    {
        avatarConfig.Character = Character;
        avatarConfig.Style = Style;
        avatarConfig.IsCustomAvatar = IsCustomAvatar;
        avatarConfig.UseBuiltInVoice = UseBuiltInVoice;
        avatarConfig.EnableSubtitles = EnableSubtitles;
        avatarConfig.EnableAutoReconnect = EnableAutoReconnect;
        avatarConfig.AudioGain = AudioGain;
        avatarConfig.UserLabel = UserLabel;
        avatarConfig.AssistantLabel = AssistantLabel;
        avatarConfig.TtsVoice = TtsVoice ?? avatarConfig.TtsVoice;
        avatarConfig.CustomVoiceEndpointId = CustomVoiceEndpointId ?? avatarConfig.CustomVoiceEndpointId;

        if (sttTtsConfig != null)
        {
            if (!string.IsNullOrWhiteSpace(TtsVoice))
                sttTtsConfig.TtsVoice = TtsVoice;
            if (!string.IsNullOrWhiteSpace(CustomVoiceEndpointId))
                sttTtsConfig.CustomVoiceEndpointId = CustomVoiceEndpointId;
        }
    }
}
