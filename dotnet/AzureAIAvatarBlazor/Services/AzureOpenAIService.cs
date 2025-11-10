using Azure;
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
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<Models.ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var endpoint = _configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
        var apiKey = _configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("Azure OpenAI API key not configured");
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("Azure OpenAI deployment name not configured");

        var credential = new AzureKeyCredential(apiKey);
        var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
        var chatClient = azureClient.GetChatClient(deploymentName);

        // Convert our ChatMessage model to OpenAI SDK ChatMessage
        var chatMessages = new List<OpenAI.Chat.ChatMessage>();
        foreach (var message in messages)
        {
            if (message.Role == "system")
            {
                chatMessages.Add(OpenAI.Chat.ChatMessage.CreateSystemMessage(message.Content));
            }
            else if (message.Role == "user")
            {
                chatMessages.Add(OpenAI.Chat.ChatMessage.CreateUserMessage(message.Content));
            }
            else if (message.Role == "assistant")
            {
                chatMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(message.Content));
            }
        }

        await foreach (var update in chatClient.CompleteChatStreamingAsync(chatMessages, cancellationToken: cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return contentPart.Text;
                }
            }
        }
    }
}
