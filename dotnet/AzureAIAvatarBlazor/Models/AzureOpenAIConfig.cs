namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Azure OpenAI configuration
/// </summary>
public class AzureOpenAIConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "You are an AI assistant that helps people find information.";
    public string? PromptProfile { get; set; }
    public bool EnforcePromptProfile { get; set; }
    public Dictionary<string, string> PromptVariables { get; set; } = new();

    /// <summary>
    /// Configuration mode: LLM, Agent-LLM, Agent-AIFoundry, Agent-MicrosoftFoundry
    /// </summary>
    public string Mode { get; set; } = "LLM";

    /// <summary>
    /// Azure AI Foundry Agent ID (required when Mode is Agent-AIFoundry)
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Azure AI Foundry Project Endpoint (required when Mode is Agent-AIFoundry)
    /// Format: https://your-project.api.azureml.ms
    /// This is different from the Azure OpenAI Endpoint used for LLM and Agent-LLM modes
    /// </summary>
    public string? AIFoundryEndpoint { get; set; }

    /// <summary>
    /// Microsoft Foundry (hypothetical) project endpoint for Agent-MicrosoftFoundry mode
    /// </summary>
    public string? MicrosoftFoundryEndpoint { get; set; }

    /// <summary>
    /// Agent name to use with Microsoft Foundry (Agent-MicrosoftFoundry)
    /// </summary>
    public string? MicrosoftFoundryAgentName { get; set; }
}
