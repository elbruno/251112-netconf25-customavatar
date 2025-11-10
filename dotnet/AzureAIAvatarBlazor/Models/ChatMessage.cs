namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Represents a chat message in the conversation
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
