using AzureAIAvatarBlazor.Components;
using AzureAIAvatarBlazor.Services;
using AzureAIAvatarBlazor.MAFFoundry;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ==============================
// Aspire-Managed Clients
// ==============================

// Note: Speech Service doesn't have Aspire component yet
// Configuration is injected via environment variables from AppHost

// Add user secrets support for development
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// ==============================
// MAF Foundry Integration
// ==============================

// Register MAF Foundry agents and chat client
// This will configure IChatClient and IEmbeddingGenerator if Microsoft Foundry endpoint is available
builder.AddMAFFoundryAgents();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register application services
builder.Services.AddScoped<AzureAIAgentService>();
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddSingleton<TelemetryService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Endpoint to test connection to Azure OpenAI
app.MapGet("/api/test-openai", async (AzureAIAgentService agentService) =>
{
    var (success, message) = await agentService.TestConnectionAsync();
    return Results.Ok(new { success, message });
});

app.Run();
