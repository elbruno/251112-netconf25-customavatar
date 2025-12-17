using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Runtime.CompilerServices;

namespace AzureAIAvatarBlazor.Services;

public class AzureAIAgentService
{
    private readonly ILogger<AzureAIAgentService> _logger;
    private readonly ConfigurationService _configService;
    private AIAgent? _agentLLM;
    private AIAgent? _agentAIFoundry;
    private AIAgent? _agentMicrosoftFoundry;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AzureAIAgentService(
        IConfiguration configuration,
        ILogger<AzureAIAgentService> logger,
        ConfigurationService configService)
    {
        _logger = logger;
        _configService = configService;
    }

    private async Task<AIAgent> GetOrCreateAgentAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            var config = _configService.GetConfiguration();
            var mode = config.AzureOpenAI.Mode ?? "LLM";

            _logger.LogInformation("Initializing AI Agent with mode: {Mode}", mode);

            if (mode == "Agent-AIFoundry")
            {
                if(_agentAIFoundry == null) {
                    _agentAIFoundry = await CreateAzureAIFoundryAgentAsync(config);
                }
                return _agentAIFoundry;
            }
            else if (mode == "Agent-LLM")
            {
                if (_agentLLM == null)
                {
                    _agentLLM = CreateLLMBasedAgent(config);
                }
                return _agentLLM;
            }
            else if (mode == "Agent-MicrosoftFoundry")
            {
                if (_agentMicrosoftFoundry == null)
                {
                    _agentMicrosoftFoundry = CreateMicrosoftFoundryBasedAgent(config);
                }
                return _agentMicrosoftFoundry;
            }
            else if (mode == "LLM")
            {
                // LLM mode does not use AIAgent wrapper
                _logger.LogInformation("LLM mode selected - no AIAgent will be created");
                throw new InvalidOperationException("LLM mode does not create an AI Agent instance through the Agent Framework.");
            }
            else
            {
                throw new InvalidOperationException($"Agent mode '{mode}' is not supported. Use 'Agent-LLM', 'Agent-AIFoundry', 'Agent-MicrosoftFoundry' or 'LLM'.");
            }
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

        // The SDK surface for Azure AI Foundry may differ in this environment.
        // Implementing full Foundry retrieval/adapter is out of scope for this change.
        _logger.LogWarning("Azure AI Foundry agent retrieval is not implemented in this build.");
        throw new NotImplementedException("Agent-AIFoundry integration is not implemented in this build. Implement retrieval using the appropriate Foundry SDK client.");
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

        var deploymentName = string.IsNullOrWhiteSpace(config.AzureOpenAI.DeploymentName) ? "gpt-5.1-chat" : config.AzureOpenAI.DeploymentName;

        _logger.LogInformation("Using Endpoint: {Endpoint}", config.AzureOpenAI.Endpoint);
        _logger.LogInformation("Using Deployment: {Deployment}", deploymentName);

        // Create Azure OpenAI client and convert to AI Agent
        var apiKey = new ApiKeyCredential(config.AzureOpenAI.ApiKey);
        var openAIClient = new AzureOpenAIClient(new Uri(config.AzureOpenAI.Endpoint), apiKey);
        var chatClient = openAIClient.GetChatClient(deploymentName);

        // Convert to IChatClient and create agent
        var instructions = config.AzureOpenAI.SystemPrompt ?? "You are Bruno Capuano. Respond in the user's language with a short answer and a friendly, approachable tone. If you don't know an answer, just say 'I don't know'.";
        var agent = chatClient.AsIChatClient().CreateAIAgent(instructions: instructions);

        _logger.LogInformation("LLM-based Agent created successfully");
        return agent;
    }

    private AIAgent CreateMicrosoftFoundryBasedAgent(Models.AvatarConfiguration config)
    {
        _logger.LogInformation("Creating Microsoft Foundry-based Agent...");

        if (string.IsNullOrEmpty(config.AzureOpenAI.MicrosoftFoundryEndpoint))
        {
            throw new InvalidOperationException("Azure Microsoft Foundry Endpoint is required for Agent-MicrosoftFoundry mode.");
        }

        if (string.IsNullOrEmpty(config.AzureOpenAI.MicrosoftFoundryAgentName))
        {
            throw new InvalidOperationException("Azure Microsoft Foundry Agent Name is required for Agent-MicrosoftFoundry mode.");
        }

        var agentName = config.AzureOpenAI.MicrosoftFoundryAgentName;
        var microsoftFoundryProjectEndpoint = config.AzureOpenAI.MicrosoftFoundryEndpoint;
        string tenantId = config.AzureOpenAI.TenantId;

        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrEmpty(tenantId))
        {
            credentialOptions = new DefaultAzureCredentialOptions()
            { TenantId = tenantId };
        }
        var tokenCredential = new DefaultAzureCredential(options: credentialOptions);

        AIProjectClient projectClient = new(
            endpoint: new Uri(microsoftFoundryProjectEndpoint),
            tokenProvider: tokenCredential);

        AIAgent agent = projectClient.GetAIAgent(agentName);

        _logger.LogInformation($"Using Microsoft Foundry Endpoint: {microsoftFoundryProjectEndpoint}");
        _logger.LogInformation($"Using Agent Name: {agentName}");

        _logger.LogInformation("Microsoft Foundry based Agent created successfully");
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

    public Task ResetAgentAsync()
    {
        _logger.LogInformation("Resetting cached agent instance");
        _agentLLM = null;
        _agentMicrosoftFoundry = null;
        _agentAIFoundry = null;
        return Task.CompletedTask;
    }
}
