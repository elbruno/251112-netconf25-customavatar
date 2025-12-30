#pragma warning disable CA2252, OPENAI001

using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureAIAvatarBlazor.MAFFoundry;

/// <summary>
/// Provides access to agents and chat clients from Microsoft Foundry.
/// Agents are pre-deployed and managed in Microsoft Foundry.
/// </summary>
public class MAFFoundryAgentProvider
{
    private readonly AIProjectClient _projectClient;
    private readonly IConfiguration _configuration;
    private readonly string _tenantId;
    
    public MAFFoundryAgentProvider(string microsoftFoundryProjectEndpoint, IConfiguration configuration, string tenantId = "")
    {
        _configuration = configuration;
        _tenantId = tenantId;

        DefaultAzureCredential tokenCredential = GetAzureCredentials();

        _projectClient = new(
            endpoint: new Uri(microsoftFoundryProjectEndpoint),
            tokenProvider: tokenCredential);
    }

    /// <summary>
    /// Gets an AI agent by its agent name from Microsoft Foundry (synchronous).
    /// </summary>
    public AIAgent GetAIAgent(string agentName, List<AITool>? tools = null)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new ArgumentException("Agent Name cannot be null or empty", nameof(agentName));
        }

        return _projectClient.GetAIAgent(name: agentName, tools: tools);
    }

    /// <summary>
    /// Gets an existing agent or creates a new one if it doesn't exist.
    /// </summary>
    public AIAgent GetOrCreateAIAgent(string agentName,
        string model = "",
        string agentInstructions = "", List<AITool>? tools = null)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            throw new ArgumentException("Agent Name cannot be null or empty", nameof(agentName));
        }
        
        AIAgent? agent = null;

        try
        {
            agent = _projectClient.GetAIAgent(name: agentName, tools: tools);
        }
        catch (Exception ex) when (ex is Azure.RequestFailedException || ex is HttpRequestException)
        {
            // Agent doesn't exist, will create it
            // Expected exceptions when agent is not found
        }

        agent ??= _projectClient.CreateAIAgent(
                name: agentName,
                model: model,
                instructions: agentInstructions,
                tools: tools);

        return agent;
    }

    /// <summary>
    /// Gets an IChatClient configured for the specified deployment.
    /// </summary>
    public IChatClient GetChatClient(string? deploymentName = null)
    {
        if (string.IsNullOrEmpty(deploymentName))
        {
            deploymentName = _configuration["AI_ChatDeploymentName"] ?? "gpt-5-mini";
        }

        var azureOpenAIChatClient = _projectClient.GetAzureOpenAIChatClient(deploymentName);

        // Get credentials        
        DefaultAzureCredential tokenCredential = GetAzureCredentials();
        var endpoint = new Uri(NormalizeEndpoint(azureOpenAIChatClient.Endpoint.AbsoluteUri));
        var azureOpenAIClient = new AzureOpenAIClient(
            endpoint: endpoint,
            credential: tokenCredential);

        return azureOpenAIClient
            .GetChatClient(deploymentName)
            .AsIChatClient();
    }

    /// <summary>
    /// Gets an embedding generator configured for the specified deployment.
    /// </summary>
    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(string? deploymentName = null)
    {
        if (string.IsNullOrEmpty(deploymentName))
        {
            deploymentName = _configuration["AI_embeddingsDeploymentName"] ?? "text-embedding-3-small";
        }
        
        var azureOpenAIEmbeddingClient = _projectClient.GetAzureOpenAIEmbeddingClient(deploymentName);

        // Get credentials        
        DefaultAzureCredential tokenCredential = GetAzureCredentials();
        var endpoint = new Uri(NormalizeEndpoint(azureOpenAIEmbeddingClient.Endpoint.AbsoluteUri));
        var azureOpenAIClient = new AzureOpenAIClient(
            endpoint: endpoint,
            credential: tokenCredential);

        return azureOpenAIClient
            .GetEmbeddingClient(deploymentName)
            .AsIEmbeddingGenerator();
    }

    private DefaultAzureCredential GetAzureCredentials()
    {
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrEmpty(_tenantId))
        {
            credentialOptions = new DefaultAzureCredentialOptions()
            { TenantId = _tenantId };
        }
        var tokenCredential = new DefaultAzureCredential(options: credentialOptions);
        return tokenCredential;
    }

    internal static string NormalizeEndpoint(string endpoint)
    {
        // If the endpoint contains ".services.ai.azure.com/api/projects/", replace with ".cognitiveservices.azure.com"
        if (endpoint.Contains(".services.ai.azure.com/api/projects/"))
        {
            var idx = endpoint.IndexOf(".services.ai.azure.com/api/projects/", StringComparison.OrdinalIgnoreCase);
            var prefix = endpoint.Substring(0, idx);
            return $"{prefix}.cognitiveservices.azure.com";
        }
        return endpoint;
    }
}

/// <summary>
/// Extension methods for registering MAF Foundry agents in dependency injection.
/// </summary>
public static class MAFFoundryAgentExtensions
{
    /// <summary>
    /// Registers MAF Foundry agent provider and services to the service collection.
    /// </summary>
    public static WebApplicationBuilder AddMAFFoundryAgents(
        this WebApplicationBuilder builder)
    {
        var projectEndpoint = builder.Configuration.GetConnectionString("microsoftfoundryproject");
        var tenantId = builder.Configuration.GetConnectionString("tenantId");

        var logger = builder.Services.BuildServiceProvider().GetService<ILoggerFactory>()?.
            CreateLogger("MAFFoundryAgentExtensions");

        // If no Foundry project endpoint is configured, skip registration to allow local runs
        if (string.IsNullOrWhiteSpace(projectEndpoint))
        {
            logger?.LogWarning("Microsoft Foundry project endpoint not configured; skipping Foundry agent registration.");
            return builder;
        }

        // Register the MAFFoundryAgentProvider as singleton
        MAFFoundryAgentProvider mafFoundryAgentProvider = new(projectEndpoint, builder.Configuration, tenantId ?? "");
        builder.Services.AddSingleton(_ => mafFoundryAgentProvider);

        // Register the IChatClient as is used in other scenarios
        IChatClient chatClient = mafFoundryAgentProvider.GetChatClient();        
        builder.Services.AddChatClient(chatClient);

        // Register the IEmbeddingGenerator as is used in other scenarios
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = mafFoundryAgentProvider.GetEmbeddingGenerator();        
        builder.Services.AddSingleton(embeddingGenerator);

        logger?.LogInformation("Registered MAF Foundry agent provider from endpoint: {Endpoint}", projectEndpoint);

        return builder;
    }
}
