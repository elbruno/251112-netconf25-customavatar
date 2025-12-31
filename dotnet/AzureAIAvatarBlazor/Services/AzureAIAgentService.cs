#pragma warning disable CA2252, OPENAI001

using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace AzureAIAvatarBlazor.Services;

public class AzureAIAgentService
{
    private readonly ILogger<AzureAIAgentService> _logger;
    private readonly ConfigurationService _configService;
    private readonly TelemetryService _telemetryService;
    private AIAgent? _agentLLM;
    private AIAgent? _agentAIFoundry;
    private AIAgent? _agentMicrosoftFoundry;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AzureAIAgentService(
        IConfiguration configuration,
        ILogger<AzureAIAgentService> logger,
        ConfigurationService configService,
        TelemetryService telemetryService)
    {
        _logger = logger;
        _configService = configService;
        _telemetryService = telemetryService;
    }

    private async Task<AIAgent> GetOrCreateAgentAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            var config = _configService.GetConfiguration();
            var mode = config.AzureOpenAI.Mode ?? "Agent-LLM";
            var deploymentName = mode switch
            {
                "Agent-LLM" => config.AzureOpenAI.AgentLLM.DeploymentName ?? "gpt-5.1-chat",
                "Agent-MicrosoftFoundry" => config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryAgentName ?? "unknown",
                _ => "unknown"
            };

            var endpoint = mode switch
            {
                "Agent-LLM" => config.AzureOpenAI.AgentLLM.Endpoint,
                "Agent-AIFoundry" => config.AzureOpenAI.AgentAIFoundry.AIFoundryEndpoint,
                "Agent-MicrosoftFoundry" => config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryEndpoint,
                _ => null
            };

            using var initActivity = _telemetryService.StartAIAgentInitSpan(mode, endpoint);

            _logger.LogInformation("Initializing AI Agent with {AgentMode} mode, Model/Agent: {ModelDeployment}",
                mode, deploymentName);

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
            else
            {
                _logger.LogError("Unsupported agent mode: {AgentMode}. Supported modes: {SupportedModes}",
                    mode, new[] { "Agent-LLM", "Agent-AIFoundry", "Agent-MicrosoftFoundry" });
                throw new InvalidOperationException($"Agent mode '{mode}' is not supported. Use 'Agent-LLM', 'Agent-AIFoundry', or 'Agent-MicrosoftFoundry'.");
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<AIAgent> CreateAzureAIFoundryAgentAsync(Models.AvatarConfiguration config)
    {
        _logger.LogInformation("Creating Azure AI Foundry Agent with AgentId: {AgentId}, Endpoint: {AIFoundryEndpoint}",
            config.AzureOpenAI.AgentAIFoundry.AgentId,
            config.AzureOpenAI.AgentAIFoundry.AIFoundryEndpoint);

        if (string.IsNullOrEmpty(config.AzureOpenAI.AgentAIFoundry.AgentId))
        {
            _logger.LogError("AgentId is missing for Agent-AIFoundry mode");
            throw new InvalidOperationException("AgentId is required for Agent-AIFoundry mode.");
        }

        if (string.IsNullOrEmpty(config.AzureOpenAI.AgentAIFoundry.AIFoundryEndpoint))
        {
            _logger.LogError("Azure AI Foundry endpoint is missing for Agent-AIFoundry mode");
            throw new InvalidOperationException("Azure AI Foundry endpoint is required for Agent-AIFoundry mode.");
        }

        _logger.LogWarning("Azure AI Foundry agent retrieval is not implemented in this build");
        throw new NotImplementedException("Agent-AIFoundry integration is not implemented in this build. Implement retrieval using the appropriate Foundry SDK client.");
    }

    private AIAgent CreateLLMBasedAgent(Models.AvatarConfiguration config)
    {
        var deploymentName = string.IsNullOrWhiteSpace(config.AzureOpenAI.AgentLLM.DeploymentName) ? "gpt-5.1-chat" : config.AzureOpenAI.AgentLLM.DeploymentName;

        _logger.LogInformation("Creating LLM-based Agent with Endpoint: {OpenAIEndpoint}, Deployment: {ModelDeployment}",
            config.AzureOpenAI.AgentLLM.Endpoint,
            deploymentName);

        if (string.IsNullOrEmpty(config.AzureOpenAI.AgentLLM.Endpoint))
        {
            _logger.LogError("Azure OpenAI Endpoint is missing for Agent-LLM mode");
            throw new InvalidOperationException("Azure OpenAI Endpoint is required for Agent-LLM mode.");
        }

        if (string.IsNullOrEmpty(config.AzureOpenAI.AgentLLM.ApiKey))
        {
            _logger.LogError("Azure OpenAI API Key is missing for Agent-LLM mode");
            throw new InvalidOperationException("Azure OpenAI API Key is required for Agent-LLM mode.");
        }

        var apiKey = new ApiKeyCredential(config.AzureOpenAI.AgentLLM.ApiKey);
        var openAIClient = new AzureOpenAIClient(new Uri(config.AzureOpenAI.AgentLLM.Endpoint), apiKey);
        var chatClient = openAIClient.GetChatClient(deploymentName);

        var instructions = config.AzureOpenAI.AgentLLM.SystemPrompt ?? "You are Pablo Piovano. Respond in the user's language with a short answer and a friendly, approachable tone. If you don't know an answer, just say 'I don't know'.";
        var agent = chatClient.AsIChatClient().CreateAIAgent(instructions: instructions);

        _logger.LogInformation("LLM-based Agent created successfully with deployment: {ModelDeployment}", deploymentName);
        return agent;
    }

    private AIAgent CreateMicrosoftFoundryBasedAgent(Models.AvatarConfiguration config)
    {
        var agentName = config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryAgentName;
        var microsoftFoundryProjectEndpoint = config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryEndpoint;

        _logger.LogInformation("Creating Microsoft Foundry-based Agent with Endpoint: {FoundryEndpoint}, AgentName: {AgentName}",
            microsoftFoundryProjectEndpoint,
            agentName);

        if (string.IsNullOrEmpty(microsoftFoundryProjectEndpoint))
        {
            _logger.LogError("Microsoft Foundry Endpoint is missing for Agent-MicrosoftFoundry mode");
            throw new InvalidOperationException("Azure Microsoft Foundry Endpoint is required for Agent-MicrosoftFoundry mode.");
        }

        if (string.IsNullOrEmpty(agentName))
        {
            _logger.LogError("Microsoft Foundry Agent Name is missing for Agent-MicrosoftFoundry mode");
            throw new InvalidOperationException("Azure Microsoft Foundry Agent Name is required for Agent-MicrosoftFoundry mode.");
        }
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
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting chat completion stream with Agent Framework");
        _logger.LogInformation("Message count: {Count}, Last message role: {Role}",
            messages.Count,
            messages.LastOrDefault()?.Role ?? "(none)");

        var agent = await GetOrCreateAgentAsync();
        var config = _configService.GetConfiguration();
        var mode = config.AzureOpenAI.Mode ?? "Agent-LLM";
        var modelOrAgent = mode switch
        {
            "Agent-LLM" => config.AzureOpenAI.AgentLLM.DeploymentName ?? "gpt-5.1-chat",
            "Agent-MicrosoftFoundry" => config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryAgentName ?? "unknown",
            _ => "unknown"
        };

        var lastUserMessage = messages.LastOrDefault(m => m.Role == "user");
        if (lastUserMessage == null)
        {
            _logger.LogWarning("No user message found in the conversation");
            yield break;
        }

        using var activity = _telemetryService.StartAIAgentChatSpan(mode, modelOrAgent, lastUserMessage.Content.Length);

        // Track chat message
        _telemetryService.TrackChatMessage("user", lastUserMessage.Content.Length);

        _logger.LogInformation("Sending message to agent: {Preview}",
            lastUserMessage.Content.Length > 100
                ? lastUserMessage.Content.Substring(0, 100) + "..."
                : lastUserMessage.Content);

        var totalChunks = 0;
        var totalCharacters = 0;

        var response = await agent.RunAsync(lastUserMessage.Content, cancellationToken: cancellationToken);

        var text = response.Text ?? string.Empty;

        if (!string.IsNullOrEmpty(text))
        {
            totalChunks = 1;
            totalCharacters = text.Length;
            yield return text;
        }

        stopwatch.Stop();
        
        // Track AI response time
        _telemetryService.TrackAIResponseTime(mode, stopwatch.ElapsedMilliseconds, totalCharacters);
        _telemetryService.TrackChatMessage("assistant", totalCharacters);

        // Add rich attributes to span
        activity?.SetTag("ai.response.chunks", totalChunks);
        activity?.SetTag("ai.response.completion_length", totalCharacters);
        activity?.SetTag("ai.response.duration_ms", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("ai.response.tokens_per_second", 
            stopwatch.ElapsedMilliseconds > 0 ? (totalCharacters / (stopwatch.ElapsedMilliseconds / 1000.0)) : 0);

        _logger.LogInformation("Agent response completed: {Chunks} chunks, {Characters} total characters",
            totalChunks, totalCharacters);
    }

    public Task ResetAgentAsync()
    {
        _agentLLM = null;
        _agentAIFoundry = null;
        _agentMicrosoftFoundry = null;
        return Task.CompletedTask;
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        using var activity = _telemetryService.StartActivity("TestConnection", ActivityKind.Client);
        
        try
        {
            _logger.LogInformation("Testing Azure OpenAI connection...");
            
            var config = _configService.GetConfiguration();
            var mode = config.AzureOpenAI.Mode ?? "Agent-LLM";
            
            activity?.SetTag("mode", mode);
            
            _logger.LogInformation("Testing connection for mode: {Mode}", mode);

            if (mode == "Agent-LLM")
            {
                if (string.IsNullOrEmpty(config.AzureOpenAI.AgentLLM.Endpoint))
                    return (false, "❌ Error: Azure OpenAI Endpoint is not configured");
                
                if (string.IsNullOrEmpty(config.AzureOpenAI.AgentLLM.ApiKey))
                    return (false, "❌ Error: Azure OpenAI API Key is not configured");

                // Try to create the agent and make a simple call
                var agent = await GetOrCreateAgentAsync();
                var response = await agent.RunAsync("Reply only: OK", cancellationToken: default);
                
                if (!string.IsNullOrEmpty(response.Text))
                {
                    activity?.SetTag("success", true);
                    return (true, $"✅ Connection successful!\n" +
                        $"Endpoint: {config.AzureOpenAI.AgentLLM.Endpoint}\n" +
                        $"Deployment: {config.AzureOpenAI.AgentLLM.DeploymentName}\n" +
                        $"Test response: {response.Text}");
                }
                
                return (false, "❌ Error: No response received from the model");
            }
            else if (mode == "Agent-MicrosoftFoundry")
            {
                if (string.IsNullOrEmpty(config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryEndpoint))
                    return (false, "❌ Error: Microsoft Foundry Endpoint is not configured");
                
                var agent = await GetOrCreateAgentAsync();
                var response = await agent.RunAsync("Reply only: OK", cancellationToken: default);

                if (!string.IsNullOrEmpty(response.Text))
                {
                    activity?.SetTag("success", true);
                    return (true, $"✅ Microsoft Foundry connection successful!\n" +
                        $"Endpoint: {config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryEndpoint}\n" +
                        $"Agent: {config.AzureOpenAI.AgentMicrosoftFoundry.MicrosoftFoundryAgentName}\n" +
                        $"Test response: {response.Text}");
                }

                return (false, "❌ Error: No response received from the Microsoft Foundry agent");
            }
            else
            {
                return (false, $"❌ Mode '{mode}' is not supported for connection testing");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Azure OpenAI connection");
            _telemetryService.TrackError("TestConnection", ex);
            activity?.SetTag("success", false);
            activity?.SetTag("error", ex.Message);
            return (false, $"❌ Connection error: {ex.Message}");
        }
    }
}
