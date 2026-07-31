using HotChocolate.Fusion.Aspire;

const string nitroCloudUrl = "https://nitro.example.invalid";
const string nitroApiId = "replace-with-nitro-api-id";

var builder = DistributedApplication.CreateBuilder(args);

builder.AddNitro();
builder.AddAzureContainerAppEnvironment("demo-aca");

// every stage this api publishes to is declared here by name. An invocation names one of them
// through the stage parameter, and the release tag identifies the release across all of them.
var stage = builder.AddParameter("stage");
var tag = builder.AddParameter("tag");
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var products = builder
    .AddProject<Projects.Products>("products")
    .WithExternalHttpEndpoints()
    .WithGraphQLSchemaFile();

var reviews = builder
    .AddProject<Projects.Reviews>("reviews")
    .WithExternalHttpEndpoints()
    .WithGraphQLSchemaFile();

var gateway = builder
    .AddProject<Projects.Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithGraphQLSchemaComposition(
        settings: new GraphQLCompositionSettings
        {
            // a publish composes for the stage it publishes to, a local run composes for "local".
            EnvironmentName = builder.ExecutionContext.IsRunMode ? "local" : null
        })
    .WithReference(products)
    .WithReference(reviews);

var nitro = builder
    .AddNitroPublishTarget("nitro")
    .WithNitroCloudUrl(nitroCloudUrl)
    .WithNitroApiId(nitroApiId)
    .WithNitroApiKey(nitroApiKey)
    .WithStageParameter(stage)
    .WithConfigurationTag(tag);

nitro.AddStage("development");
nitro.AddStage("test");

if (!builder.ExecutionContext.IsRunMode)
{
    var nitroGatewayApiKey = builder.AddParameter(
        "nitroGatewayApiKey",
        secret: true);

    gateway
        .WithEnvironment("NITRO_URL", nitroCloudUrl)
        .WithEnvironment("NITRO_API_ID", nitroApiId)
        .WithEnvironment("NITRO_STAGE", stage)
        .WithEnvironment("NITRO_API_KEY", nitroGatewayApiKey);
}

builder.Build().Run();
