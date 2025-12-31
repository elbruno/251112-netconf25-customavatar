using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureAIAvatarBlazor.MAFLocal;

/// <summary>
/// Provides locally-created agents using the Microsoft Agent Framework.
/// Agents are created with instructions and tools configured locally using IChatClient.
/// </summary>
public class MAFLocalAgentProvider
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the MAFLocalAgentProvider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving agents.</param>
    /// <param name="chatClient">The chat client to use for creating agents (not stored, passed during registration).</param>
    public MAFLocalAgentProvider(
        IServiceProvider serviceProvider,
        IChatClient chatClient)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets an agent by name string.
    /// </summary>
    public AIAgent GetAgentByName(string agentName)
    {
        return _serviceProvider.GetRequiredKeyedService<AIAgent>(agentName);
    }
}

/// <summary>
/// Extension methods for registering local MAF agents in dependency injection.
/// Follows the pattern of AddAIAgent(name, factory) for individual agent registration.
/// </summary>
public static class MAFLocalAgentExtensions
{
    /// <summary>
    /// Registers a local MAF agent with the specified name and instructions.
    /// Each agent is registered as a keyed singleton AIAgent service.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="agentName">The unique name for the agent.</param>
    /// <param name="instructions">The system instructions for the agent.</param>
    public static WebApplicationBuilder AddMAFLocalAgent(
        this WebApplicationBuilder builder,
        string agentName,
        string instructions)
    {
        var logger = builder.Services.BuildServiceProvider().GetService<ILoggerFactory>()?.
            CreateLogger("MAFLocalAgentExtensions");

        logger?.LogInformation(
            "Registering MAF Local agent: {AgentName}",
            agentName);

        // Register the agent as keyed singleton
        builder.AddAIAgent(agentName, (sp, key) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            return chatClient.CreateAIAgent(
                name: agentName,
                instructions: instructions);
        });

        logger?.LogDebug($"Registered MAF Local agent: {agentName} as keyed singleton");
        
        return builder;
    }

    /// <summary>
    /// Registers the MAFLocalAgentProvider as a singleton service.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static WebApplicationBuilder AddMAFLocalAgentProvider(
        this WebApplicationBuilder builder)
    {
        var logger = builder.Services.BuildServiceProvider().GetService<ILoggerFactory>()?.
            CreateLogger("MAFLocalAgentExtensions");
        
        logger?.LogInformation("Registering MAFLocalAgentProvider");

        // Register the provider as singleton
        builder.Services.AddSingleton<MAFLocalAgentProvider>(sp =>
        {
            var serviceLogger = sp.GetService<ILoggerFactory>()?.
                CreateLogger("MAFLocalAgentExtensions");
            
            serviceLogger?.LogInformation("Creating MAFLocalAgentProvider with IChatClient");

            var chatClient = sp.GetRequiredService<IChatClient>();
            return new MAFLocalAgentProvider(sp, chatClient);
        });

        return builder;
    }
}
