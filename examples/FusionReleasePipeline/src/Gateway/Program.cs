using ChilliCream.Nitro;
using ChilliCream.Nitro.Fusion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("Fusion");

var nitroApiId = builder.Configuration["NITRO_API_ID"];
var nitroApiKey = builder.Configuration["NITRO_API_KEY"];
var nitroStage = builder.Configuration["NITRO_STAGE"];
var nitroUrl = builder.Configuration["NITRO_URL"];
var useNitro = !string.IsNullOrWhiteSpace(nitroApiId)
    || !string.IsNullOrWhiteSpace(nitroApiKey)
    || !string.IsNullOrWhiteSpace(nitroStage)
    || !string.IsNullOrWhiteSpace(nitroUrl);

if (useNitro)
{
    if (string.IsNullOrWhiteSpace(nitroApiId)
        || string.IsNullOrWhiteSpace(nitroApiKey)
        || string.IsNullOrWhiteSpace(nitroStage)
        || string.IsNullOrWhiteSpace(nitroUrl))
    {
        throw new InvalidOperationException(
            "NITRO_URL, NITRO_API_ID, NITRO_API_KEY, and NITRO_STAGE "
            + "must all be configured for a deployed gateway.");
    }

    builder.Services
        .AddNitro(options =>
        {
            options.ApiId = nitroApiId;
            options.ApiKey = nitroApiKey;
            options.Stage = nitroStage;
            options.ServerUrl = nitroUrl;
        })
        .AddFusion();
}
else
{
    builder
        .AddGraphQLGateway()
        .AddFileSystemConfiguration("gateway.far");
}

var app = builder.Build();

app.MapGraphQLHttp();

app.Run();
