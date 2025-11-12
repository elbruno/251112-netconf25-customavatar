using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Runtime.CompilerServices;

namespace AzureAIAvatarBlazor.Services;

public class AzureAIAgentService : IAzureAIAgentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureAIAgentService> _logger;
    private readonly IConfigurationService _configService;
    private AIAgent? _agent;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AzureAIAgentService(
        IConfiguration configuration,
        ILogger<AzureAIAgentService> logger,
        IConfigurationService configService)
    {
        _configuration = configuration;
        _logger = logger;
        _configService = configService;
    }

    private async Task<AIAgent> GetOrCreateAgentAsync()
    {
        if (_agent != null)
        {
            return _agent;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_agent != null)
            {
                return _agent;
            }

            var config = _configService.GetConfiguration();
            var mode = config.AzureOpenAI.Mode ?? "LLM";

            _logger.LogInformation("Initializing AI Agent with mode: {Mode}", mode);

            if (mode == "Agent-AIFoundry")
            {
                _agent = await CreateAzureAIFoundryAgentAsync(config);
            }
            else if (mode == "Agent-LLM")
            {
                _agent = CreateLLMBasedAgent(config);
            }
            else
            {
                throw new InvalidOperationException($"Agent mode '{mode}' is not supported. Use 'Agent-LLM' or 'Agent-AIFoundry'.");
            }

            _logger.LogInformation("AI Agent initialized successfully");
            return _agent;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<AIAgent> CreateAzureAIFoundryAgentAsync(Models.AvatarConfiguration config)
    {
        _logger.LogInformation("Creating Azure AI Foundry Agent...");

        if (string.IsNullOrEmpty(config.AzureOpenAI.AgentId))
        {
            throw new InvalidOperationException("AgentId is required for Agent-AIFoundry mode.");
        }

        if (string.IsNullOrEmpty(config.AzureOpenAI.AIFoundryEndpoint))
        {
            throw new InvalidOperationException("Azure AI Foundry endpoint is required for Agent-AIFoundry mode.");
        }

        _logger.LogInformation("Using Azure AI Foundry Agent ID: {AgentId}", config.AzureOpenAI.AgentId);
        _logger.LogInformation("Using AI Foundry Endpoint: {Endpoint}", config.AzureOpenAI.AIFoundryEndpoint);

        // Create the persistent agent client using Azure credentials
        var credential = new DefaultAzureCredential();
        var persistentAgentClient = new PersistentAgentsClient(config.AzureOpenAI.AIFoundryEndpoint, credential);

        // Get the existing agent by ID
        var agent = await persistentAgentClient.GetAIAgentAsync(config.AzureOpenAI.AgentId);

        _logger.LogInformation("Azure AI Foundry Agent retrieved successfully");
        return agent;
    }

    private AIAgent CreateLLMBasedAgent(Models.AvatarConfiguration config)
    {
        _logger.LogInformation("Creating LLM-based Agent...");

        if (string.IsNullOrEmpty(config.AzureOpenAI.Endpoint))
        {
            throw new InvalidOperationException("Azure OpenAI Endpoint is required for Agent-LLM mode.");
        }

        if (string.IsNullOrEmpty(config.AzureOpenAI.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI API Key is required for Agent-LLM mode.");
        }

        var deploymentName = config.AzureOpenAI.DeploymentName ?? "gpt-4o-mini";

        _logger.LogInformation("Using Endpoint: {Endpoint}", config.AzureOpenAI.Endpoint);
        _logger.LogInformation("Using Deployment: {Deployment}", deploymentName);

        // Create Azure OpenAI client and convert to AI Agent
        var apiKey = new ApiKeyCredential(config.AzureOpenAI.ApiKey);
        var openAIClient = new AzureOpenAIClient(new Uri(config.AzureOpenAI.Endpoint), apiKey);
        var chatClient = openAIClient.GetChatClient(deploymentName);

        // Convert to IChatClient and create agent
        var instructions = config.AzureOpenAI.SystemPrompt ?? "You are Bruno Capuano. Respond in the user's language with a short answer and a friendly, approachable tone. Convert numeric times (e.g., 08:00) to spoken format (e.g., \"eight in the morning\"). If you don't know an answer, just say \"I don't know\"";
        var agent = chatClient.AsIChatClient().CreateAIAgent(instructions: instructions);

        _logger.LogInformation("LLM-based Agent created successfully");
        return agent;
    }

    public async IAsyncEnumerable<string> GetChatCompletionStreamAsync(
        List<Models.ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting chat completion stream with Agent Framework");
        _logger.LogInformation("Message count: {Count}, Last message role: {Role}",
            messages.Count,
            messages.LastOrDefault()?.Role ?? "(none)");

        var agent = await GetOrCreateAgentAsync();

        // Get the last user message
        var lastUserMessage = messages.LastOrDefault(m => m.Role == "user");
        if (lastUserMessage == null)
        {
            _logger.LogWarning("No user message found in the conversation");
            yield break;
        }

        _logger.LogInformation("Sending message to agent: {Preview}",
            lastUserMessage.Content.Length > 100
                ? lastUserMessage.Content.Substring(0, 100) + "..."
                : lastUserMessage.Content);

        var totalChunks = 0;
        var totalCharacters = 0;

        // Use the agent's RunAsync method
        // The Agent Framework doesn't currently support streaming in the same way as the OpenAI SDK
        var response = await agent.RunAsync(lastUserMessage.Content, cancellationToken: cancellationToken);

        // The response.Text contains the full response
        var text = response.Text ?? string.Empty;

        if (!string.IsNullOrEmpty(text))
        {
            totalChunks = 1;
            totalCharacters = text.Length;
            yield return text;
        }

        _logger.LogInformation("Agent response completed: {Chunks} chunks, {Characters} total characters",
            totalChunks, totalCharacters);
    }
}
