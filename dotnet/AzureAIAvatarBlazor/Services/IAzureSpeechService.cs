namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Service for interacting with Azure Speech Service (STT/TTS/Avatar)
/// Note: Most of the Speech SDK functionality for avatar needs to run in the browser
/// using JavaScript interop due to WebRTC and media stream requirements
/// </summary>
public interface IAzureSpeechService
{
    Task<bool> ValidateConnectionAsync();
    string GetRegion();
    string GetSubscriptionKey();
}
