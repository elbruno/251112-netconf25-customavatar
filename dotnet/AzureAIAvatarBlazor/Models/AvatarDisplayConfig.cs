namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Avatar display configuration
/// </summary>
public class AvatarDisplayConfig
{
    /// <summary>
    /// Standard Azure TTS avatars that don't require custom configuration
    /// </summary>
    public static readonly string[] StandardAvatars = ["lisa", "harry", "jeff", "lori", "max", "meg"];

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

    /// <summary>
    /// Returns true if the current character is a standard Azure avatar
    /// </summary>
    public bool IsStandardCharacter => 
        StandardAvatars.Contains(Character?.ToLowerInvariant() ?? string.Empty);

    /// <summary>
    /// Auto-detects if this should be treated as a custom avatar based on character name
    /// </summary>
    public static bool IsCustomAvatarCharacter(string character)
    {
        if (string.IsNullOrWhiteSpace(character)) return false;
        return !StandardAvatars.Contains(character.ToLowerInvariant());
    }
}
