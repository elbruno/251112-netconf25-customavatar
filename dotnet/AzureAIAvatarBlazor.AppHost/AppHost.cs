var builder = DistributedApplication.CreateBuilder(args);

// ==============================
// Azure Application Insights
// ==============================

// Add Application Insights for monitoring and telemetry
// In development: Uses local connection string or creates new resource
// In production: Provisions Application Insights in Azure
var insights = builder.AddAzureApplicationInsights("appinsights");

// ==============================
// Azure OpenAI
// ==============================

IResourceBuilder<IResourceWithConnectionString>? microsoftfoundrycnnstring = builder.AddConnectionString("microsoftfoundrycnnstring");


// ==============================
// Application Project
// ==============================

var avatarApp = builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WithReference(microsoftfoundrycnnstring)
    .WithReference(insights);

// ==============================
// Build and Run
// ==============================

builder.Build().Run();
