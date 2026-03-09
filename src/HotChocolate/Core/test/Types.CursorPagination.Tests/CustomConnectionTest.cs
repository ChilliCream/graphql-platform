using System.Collections.Immutable;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class CustomConnectionTest
{
    [Fact]
    public async Task Ensure_That_Custom_Connections_Work_With_Legacy_Middleware()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ products { edges { node { name } } } }")
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Query
    {
        [UsePaging]
        public Task<ProductConnection> GetProductsAsync(
            int? first, string? after, int? last, string? before)
            => Task.FromResult(
                new ProductConnection(
                    [new ProductEdge(new Product("Abc"))],
                    new ConnectionPageInfo(true, true, "Abc", "Abc")));
    }

    public class ProductConnection(
        ImmutableArray<ProductEdge> edges,
        ConnectionPageInfo pageInfo)
        : ConnectionBase<Product, ProductEdge, ConnectionPageInfo>
    {
        public override IReadOnlyList<ProductEdge> Edges { get; } = edges;
        public override ConnectionPageInfo PageInfo { get; } = pageInfo;
    }

    public class ProductEdge(Product product) : IEdge<Product>
    {
        public string Cursor => product.Name;
        public Product Node => product;
        object? IEdge.Node => Node;
    }

    public record Product(string Name);
}
