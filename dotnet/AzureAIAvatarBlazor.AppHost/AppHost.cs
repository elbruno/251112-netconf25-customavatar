var builder = DistributedApplication.CreateBuilder(args);

// ==============================
// Azure AI Resource Definitions
// ==============================

// Azure OpenAI
// Conditional: Use provisioned resource in publish mode, connection string in dev
var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
           .AddDeployment("chat", "gpt-4o-mini", "2024-11-01")
    : builder.AddConnectionString("openai");

// Azure Speech Service
// Note: Aspire doesn't have first-class Speech resource yet, use connection string
var speech = builder.AddConnectionString("speech");

// Azure Cognitive Search (Optional - only if configured)
var searchEndpoint = builder.Configuration["ConnectionStrings:search"];
IResourceBuilder<IResourceWithConnectionString>? search = null;
if (!string.IsNullOrEmpty(searchEndpoint))
{
    search = builder.ExecutionContext.IsPublishMode
        ? builder.AddAzureSearch("search")
        : builder.AddConnectionString("search");
}

// ==============================
// Application Project
// ==============================

var avatarApp = builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WithReference(openai)
    .WithReference(speech)
    .WithEnvironment("Avatar__Character", builder.Configuration["Avatar:Character"] ?? "lisa")
    .WithEnvironment("Avatar__Style", builder.Configuration["Avatar:Style"] ?? "casual-sitting")
    .WithEnvironment("OpenAI__DeploymentName", builder.Configuration["OpenAI:DeploymentName"] ?? "gpt-4o-mini")
    .WithEnvironment("SystemPrompt", builder.Configuration["SystemPrompt"] ?? "You are a helpful AI assistant.");

// Add search reference if configured
if (search != null)
{
    avatarApp.WithReference(search);
}

// ==============================
// Build and Run
// ==============================

builder.Build().Run();
