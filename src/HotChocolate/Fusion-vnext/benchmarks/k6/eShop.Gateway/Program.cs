using HotChocolate.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("Fusion");

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far")
    .ModifyRequestOptions(o => o.CollectOperationPlanTelemetry = true);

var app = builder.Build();

#if RELEASE
app.MapGraphQLHttp()
#else
app.MapGraphQL().WithOptions(new GraphQLServerOptions { Tool = { ServeMode = GraphQLToolServeMode.Insider } });
#endif

app.Run();
