namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Service for interacting with Azure AI Agents (Agent Framework)
/// </summary>
public interface IAzureAIAgentService
{
    IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<Models.ChatMessage> messages,
        CancellationToken cancellationToken = default);
}
