#pragma warning disable CA2252, OPENAI001

using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureAIAvatarBlazor.MAFLocal;

/// <summary>
/// Provides local agent creation using IChatClient without requiring pre-deployed agents.
/// This provider is designed for Agent-LLM mode where agents are created on-demand.
/// </summary>
public class MAFLocalAgentProvider
{
    private readonly IChatClient _chatClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MAFLocalAgentProvider>? _logger;

    public MAFLocalAgentProvider(IChatClient chatClient, IConfiguration configuration, ILogger<MAFLocalAgentProvider>? logger = null)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
    }

    /// <summary>
    /// Creates a new AI agent using the provided IChatClient with the specified instructions.
    /// This is suitable for local development and Agent-LLM scenarios.
    /// </summary>
    /// <param name="instructions">The system prompt/instructions for the agent</param>
    /// <param name="name">Optional name for the agent (for logging purposes)</param>
    /// <param name="tools">Optional tools to attach to the agent</param>
    /// <returns>An AIAgent instance</returns>
    public AIAgent CreateAIAgent(string instructions, string? name = null, List<AITool>? tools = null)
    {
        if (string.IsNullOrWhiteSpace(instructions))
        {
            instructions = "You are a helpful AI assistant. Respond in the user's language with a short answer and a friendly, approachable tone.";
        }

        _logger?.LogInformation("Creating local AI Agent with name: {Name}", name ?? "DefaultAgent");

        var agent = _chatClient.CreateAIAgent(instructions: instructions, tools: tools);

        _logger?.LogInformation("Local AI Agent created successfully");

        return agent;
    }

    /// <summary>
    /// Gets the underlying IChatClient used by this provider.
    /// </summary>
    public IChatClient GetChatClient() => _chatClient;
}

/// <summary>
/// Extension methods for registering MAF Local agent provider in dependency injection.
/// </summary>
public static class MAFLocalAgentExtensions
{
    /// <summary>
    /// Registers MAF Local agent provider to the service collection.
    /// This requires an IChatClient to be already registered in DI.
    /// </summary>
    public static WebApplicationBuilder AddMAFLocalAgents(
        this WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetService<ILoggerFactory>()?.
            CreateLogger("MAFLocalAgentExtensions");

        logger?.LogInformation("Registering MAF Local agent provider for Agent-LLM mode");

        // Register the MAFLocalAgentProvider as a singleton
        // It requires IChatClient to be already registered in DI
        builder.Services.AddSingleton<MAFLocalAgentProvider>(serviceProvider =>
        {
            var chatClient = serviceProvider.GetRequiredService<IChatClient>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var loggerInstance = serviceProvider.GetService<ILogger<MAFLocalAgentProvider>>();

            return new MAFLocalAgentProvider(chatClient, configuration, loggerInstance);
        });

        logger?.LogInformation("MAF Local agent provider registered successfully");

        return builder;
    }
}
