namespace AzureAIAvatarBlazor.Services;

/// <summary>
/// Service for interacting with Azure OpenAI
/// </summary>
public interface IAzureOpenAIService
{
    IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<Models.ChatMessage> messages,
        CancellationToken cancellationToken = default);
}
