using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue6487ReproTests
{
    [Fact]
    public async Task Projection_On_Record_Type_Does_Not_Throw_Default_Constructor_Error()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              products {
                id
                price {
                  amount
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    public class Query
    {
        [UseProjection]
        public IQueryable<Product> Products
            => new[]
            {
                new Product(1, new PriceData(12.34m, "USD"))
            }.AsQueryable();
    }

    public record Product(int Id, PriceData Price);

    public record PriceData(decimal Amount, string Currency);
}
