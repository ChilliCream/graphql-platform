var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHttpClient("Fusion");

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far")
    .ModifyRequestOptions(o => o.CollectOperationPlanTelemetry = true);

var app = builder.Build();

app.MapGraphQL();

app.Run();
