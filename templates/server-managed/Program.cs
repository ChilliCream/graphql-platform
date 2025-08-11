var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL()
    .AddTypes()
    .AddInstrumentation()
    .AddNitro(options =>
    {
        options.ApiId = "YOUR_API_ID";
        options.Stage = "prod";
        options.ApiKey = "YOUR_API_KEY";
    });

builder.Services
    .AddNitroTelemetry(options =>
    {
        options.ApiId = "YOUR_API_ID";
        options.Stage = "prod";
        options.ApiKey = "YOUR_API_KEY";
    });

builder.Services
    .AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddAspNetCoreInstrumentation();
        builder.AddNitroExporter();
    })
    .WithLogging(builder =>
    {
        builder.AddNitroExporter();
    });

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
