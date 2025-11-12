using Azure.AI.OpenAI;
using System.Runtime.CompilerServices;

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

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly AzureOpenAIClient _client; // Injected by Aspire
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(
        AzureOpenAIClient client, 
        IConfiguration configuration,
        ILogger<AzureOpenAIService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<Models.ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get deployment name from environment (injected by AppHost)
        var deploymentName = _configuration["OpenAI__DeploymentName"]
            ?? _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
            ?? _configuration["AzureOpenAI:DeploymentName"]
            ?? "gpt-4o-mini";

        _logger.LogInformation("Starting chat completion stream with deployment: {Deployment}", deploymentName);
        _logger.LogInformation("Message count: {Count}, Last message role: {Role}",
            messages.Count,
            messages.LastOrDefault()?.Role ?? "(none)");

        var chatClient = _client.GetChatClient(deploymentName);

        // Convert our ChatMessage model to OpenAI SDK ChatMessage
        var chatMessages = new List<OpenAI.Chat.ChatMessage>();
        foreach (var message in messages)
        {
            if (message.Role == "system")
            {
                chatMessages.Add(OpenAI.Chat.ChatMessage.CreateSystemMessage(message.Content));
                _logger.LogDebug("Added system message ({Length} chars)", message.Content.Length);
            }
            else if (message.Role == "user")
            {
                chatMessages.Add(OpenAI.Chat.ChatMessage.CreateUserMessage(message.Content));
                _logger.LogDebug("Added user message: {Preview}",
                    message.Content.Length > 100 ? message.Content.Substring(0, 100) + "..." : message.Content);
            }
            else if (message.Role == "assistant")
            {
                chatMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(message.Content));
                _logger.LogDebug("Added assistant message ({Length} chars)", message.Content.Length);
            }
        }

        var totalChunks = 0;
        var totalCharacters = 0;

        _logger.LogInformation("Sending request to Azure OpenAI...");

        // Note: Cannot use try-catch around yield return in C#
        // Error handling is done at the caller level
        await foreach (var update in chatClient.CompleteChatStreamingAsync(chatMessages, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    totalChunks++;
                    totalCharacters += contentPart.Text.Length;
                    yield return contentPart.Text;
                }
            }
        }

        _logger.LogInformation("Chat completion stream finished: {Chunks} chunks, {Characters} total characters",
            totalChunks, totalCharacters);
    }
}
