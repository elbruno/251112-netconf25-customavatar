namespace AzureAIAvatarBlazor.Models;

public class AvatarProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
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
