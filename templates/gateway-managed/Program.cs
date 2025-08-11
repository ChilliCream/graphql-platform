var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHeaderPropagation(c =>
    {
        c.Headers.Add("GraphQL-Preflight");
        c.Headers.Add("Authorization");
    });

builder.Services
    .AddHttpClient("fusion")
    .AddHeaderPropagation();

builder.Services
    .AddGraphQLGatewayServer()
    .ConfigureFromCloud(options =>
    {
        options.ApiId = "YOUR_API_ID";
        options.Stage = "prod";
        options.ApiKey = "YOUR_API_KEY";
    })
    // TODO: This needs to go
    .CoreBuilder.AddInstrumentation();

builder.Services.AddNitroTelemetry(options =>
{
    options.ApiId = "YOUR_API_ID";
    options.Stage = "prod";
    options.ApiKey = "YOUR_API_KEY";
});

builder.Services
    .AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddHttpClientInstrumentation();
        builder.AddAspNetCoreInstrumentation();
        builder.AddNitroExporter();
    })
    .WithLogging(builder =>
    {
        builder.AddNitroExporter();
    });

var app = builder.Build();

app.UseHeaderPropagation();
app.MapGraphQL();

app.Run();
