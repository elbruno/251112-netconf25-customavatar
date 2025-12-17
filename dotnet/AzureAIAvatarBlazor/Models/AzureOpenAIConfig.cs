namespace AzureAIAvatarBlazor.Models;

/// <summary>
/// Azure OpenAI configuration organized by mode sections
/// </summary>
public class AzureOpenAIConfig
{
    public string Mode { get; set; } = "Agent-LLM";

    public AgentLLMConfig AgentLLM { get; set; } = new();
    public AgentAIFoundryConfig AgentAIFoundry { get; set; } = new();
    public AgentMicrosoftFoundryConfig AgentMicrosoftFoundry { get; set; } = new();

    public string TenantId { get; set; } = string.Empty;
}

public class AgentLLMConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "You are an AI assistant that helps people find information.";
}

public class AgentAIFoundryConfig
{
    public string AIFoundryEndpoint { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
}

public class AgentMicrosoftFoundryConfig
{
    public string MicrosoftFoundryEndpoint { get; set; } = string.Empty;
    public string MicrosoftFoundryAgentName { get; set; } = string.Empty;
}
