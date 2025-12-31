var builder = DistributedApplication.CreateBuilder(args);

// ==============================
// Azure Application Insights
// ==============================

// Application Insights for telemetry
// DEVELOPMENT: Uses connection string from configuration
// PRODUCTION: Provisions Application Insights in Azure
IResourceBuilder<IResourceWithConnectionString>? appInsights;

if (builder.ExecutionContext.IsPublishMode)
{
    // PRODUCTION: Use Azure-provisioned services
    appInsights = builder.AddAzureApplicationInsights("appinsights");
}
else
{
    // DEVELOPMENT: Use connection strings from configuration
    appInsights = builder.AddConnectionString("appinsights", "APPLICATIONINSIGHTS_CONNECTION_STRING");
}

// ==============================
// Redis Cache
// ==============================

// Redis for caching configuration, conversation history, and avatar state
// DEVELOPMENT: Local Redis container
// PRODUCTION: Azure Cache for Redis
var redis = builder.AddRedis("cache")
    .WithLifetime(ContainerLifetime.Persistent); // Keep data between runs in dev

// ==============================
// Microsoft Foundry Configuration
// ==============================

// Microsoft Foundry project connection - used for agent services
IResourceBuilder<IResourceWithConnectionString>? microsoftfoundryproject;
microsoftfoundryproject = builder.AddConnectionString("microsoftfoundryproject");

// TenantId - used for agent services with Azure credentials
IResourceBuilder<IResourceWithConnectionString>? tenantId;
tenantId = builder.AddConnectionString("tenantId");

// ==============================
// Application Project
// ==============================

var avatarApp = builder.AddProject<Projects.AzureAIAvatarBlazor>("azureaiavatarblazor")
    .WaitFor(redis)
    .WithReference(redis)
    .WithReference(microsoftfoundryproject)
    .WithReference(tenantId)
    .WithReference(appInsights);

// ==============================
// Build and Run
// ==============================

builder.Build().Run();
