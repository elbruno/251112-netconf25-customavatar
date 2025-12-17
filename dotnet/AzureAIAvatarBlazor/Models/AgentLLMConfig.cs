namespace AzureAIAvatarBlazor.Models;

public class AgentLLMConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "You are an AI assistant that helps people find information.";
}
