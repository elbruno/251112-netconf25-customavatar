namespace AzureAIAvatarBlazor.Models;

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
