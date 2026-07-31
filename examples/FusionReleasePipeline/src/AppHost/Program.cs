using HotChocolate.Fusion.Aspire;

const string nitroCloudUrl = "https://nitro.example.invalid";
const string nitroApiId = "replace-with-nitro-api-id";
const string developmentStage = "replace-with-development-stage";
const string testStage = "replace-with-test-stage";

var builder = DistributedApplication.CreateBuilder(args);

builder.AddNitro();
builder.AddAzureContainerAppEnvironment("demo-aca");

var compositionEnvironment = builder.ExecutionContext.IsRunMode
    ? "local"
    : builder.Environment.EnvironmentName switch
    {
        "Development" => "development",
        "Test" => "test",
        _ => "local"
    };

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
            EnvironmentName = compositionEnvironment
        })
    .WithReference(products)
    .WithReference(reviews);

var tag = builder.AddParameter("tag");
var nitroApiKey = builder.AddParameter("nitroApiKey", secret: true);

var nitro = builder
    .AddNitroPublishTarget("nitro")
    .WithNitroCloudUrl(nitroCloudUrl)
    .WithNitroApiId(nitroApiId)
    .WithNitroApiKey(nitroApiKey);

nitro
    .AddFusionDeployment("fusion-development")
    .ForEnvironment("Development")
    .ToStage(developmentStage)
    .WithCompositionEnvironment("development")
    .WithConfigurationTag(tag);

nitro
    .AddFusionDeployment("fusion-test")
    .ForEnvironment("Test")
    .ToStage(testStage)
    .WithCompositionEnvironment("test")
    .WithConfigurationTag(tag);

var stage = compositionEnvironment switch
{
    "development" => developmentStage,
    "test" => testStage,
    _ => null
};

if (stage is not null)
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
