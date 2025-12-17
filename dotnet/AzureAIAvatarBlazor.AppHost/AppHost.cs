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
    .WithEnvironment("Avatar__Character", builder.Configuration["Avatar:Character"] ?? "Bruno-Avatar-02")
    .WithEnvironment("Avatar__Style", builder.Configuration["Avatar:Style"] ?? "")
    .WithEnvironment("Avatar__IsCustomAvatar", builder.Configuration["Avatar:IsCustomAvatar"] ?? "true")
    .WithEnvironment("Avatar__UseBuiltInVoice", builder.Configuration["Avatar:UseBuiltInVoice"] ?? "true")
    .WithEnvironment("OpenAI__DeploymentName", builder.Configuration["OpenAI:DeploymentName"] ?? "gpt-5.1-chat")
    .WithEnvironment("SystemPrompt", builder.Configuration["SystemPrompt"] ?? "You are Bruno Capuano. Respond in the user's language with a short answer and a friendly, approachable tone. Convert numeric times (e.g., 08:00) to spoken format (e.g., \"eight in the morning\"). If you don't know an answer, just say \"I don't know\".");

// Add search reference if configured
if (search != null)
{
    avatarApp.WithReference(search);
}

// ==============================
// Build and Run
// ==============================

builder.Build().Run();
