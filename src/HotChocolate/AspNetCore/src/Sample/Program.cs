var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer("test")
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

await app.RunWithGraphQLCommandsAsync(args);

public class Query
{
    public string Foo => "Foo";
}
