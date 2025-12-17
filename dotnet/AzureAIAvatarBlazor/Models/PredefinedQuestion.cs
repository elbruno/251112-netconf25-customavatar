namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Represents a single predefined question with a short title and full question text
/// </summary>
public class PredefinedQuestion
{
    /// <summary>
    /// Short title displayed on the button
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full question text sent to the chat service
    /// </summary>
    public string Question { get; set; } = string.Empty;
}
