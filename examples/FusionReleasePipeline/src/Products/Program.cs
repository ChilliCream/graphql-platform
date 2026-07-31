using HotChocolate.Types.Relay;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);

public sealed class Query
{
    private static readonly Product[] s_products =
    [
        new("p-1", "Mechanical Keyboard", 149.00),
        new("p-2", "GraphQL Mug", 18.50)
    ];

    public IReadOnlyList<Product> GetProducts() => s_products;

    public Product? GetProduct([ID] string id)
        => s_products.FirstOrDefault(product => product.Id == id);
}

public sealed record Product(
    [property: ID] string Id,
    string Name,
    double Price);
