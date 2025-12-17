namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Service for interacting with Azure AI Agents (Agent Framework)
/// </summary>
public interface IAzureAIAgentService
{
    IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<Models.ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset internal agent instance so a new mode/credentials can be picked up on next request
    /// </summary>
    Task ResetAgentAsync();
}
