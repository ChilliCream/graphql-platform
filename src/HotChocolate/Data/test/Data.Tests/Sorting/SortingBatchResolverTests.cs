using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class SortingBatchResolverTests
{
    // REPRO (known break, issue: sorting middleware is dead on batch fields).
    // [UseSorting] on a [BatchResolver] field adds the `order` argument but the compiled
    // sort middleware sits in the regular pipeline that batch selections never execute, so
    // the argument is coerced and silently ignored. A default user expects each parent's
    // result list to be ordered. This fails today (source order is returned) and is the
    // acceptance test for the fix.
    [Fact]
    public async Task UseSorting_Should_Order_PerParentResults_When_FieldIsBatchResolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    products(order: [{ name: DESC }]) {
                        name
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "products": [
                      {
                        "name": "P2"
                      },
                      {
                        "name": "P1"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "products": [
                      {
                        "name": "P2"
                      },
                      {
                        "name": "P1"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task UseSorting_Should_Expose_OrderArgument_When_FieldIsBatchResolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        var brandType = executor.Schema.Types.GetType<ObjectType>("Brand");
        var products = brandType.Fields["products"];
        var order = products.Arguments["order"];

        // assert
        Assert.Equal("ProductSortInput", order.Type.NamedType().Name);
    }

    public class Query
    {
        public List<Brand> GetBrands()
            =>
            [
                new(1, "Brand 1"),
                new(2, "Brand 2")
            ];
    }

    [ExtendObjectType<Brand>]
    public class BrandExtensions
    {
        [UseSorting]
        [BatchResolver]
        public List<Product[]> GetProducts([Parent] List<Brand> brands)
        {
            var result = new List<Product[]>(brands.Count);

            foreach (var unused in brands)
            {
                result.Add([new Product("P1"), new Product("P2")]);
            }

            return result;
        }
    }

    public record Brand(int Id, string Name);

    public record Product(string Name);
}
