using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

public class OffsetPagingBatchResolverTests
{
    // REPRO (known break, issue: offset paging partitioner reads cursor argument names).
    // PagingHelper assigns the cursor BatchPartitionKeyResolver unconditionally, even for
    // offset fields. The partitioner only runs with 2+ parents and starts by reading the
    // `first` argument, which does not exist on an offset field, so it throws. The engine now
    // isolates a throwing partitioner per context, so each brand independently reports its own
    // path-scoped error and gets a null page instead of one batch-wide fault. A default user
    // expects each brand to receive its own offset-sliced segment. This fails today.
    [Fact]
    public async Task UseOffsetPaging_Should_Slice_PerParent_When_FieldIsBatchResolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    products(take: 1) {
                        items {
                            name
                        }
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
                    "products": {
                      "items": [
                        {
                          "name": "Brand 1 P1"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "products": {
                      "items": [
                        {
                          "name": "Brand 2 P1"
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """);
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
        [UseOffsetPaging]
        [BatchResolver]
        public List<List<Product>> GetProducts([Parent] List<Brand> brands)
        {
            var result = new List<List<Product>>(brands.Count);

            foreach (var brand in brands)
            {
                result.Add(
                [
                    new Product($"Brand {brand.Id} P1"),
                    new Product($"Brand {brand.Id} P2")
                ]);
            }

            return result;
        }
    }

    public record Brand(int Id, string Name);

    public record Product(string Name);
}
