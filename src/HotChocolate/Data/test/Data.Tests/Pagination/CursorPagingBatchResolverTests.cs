using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

public class CursorPagingBatchResolverTests
{
    // PROVES-WORKS (classified unknown-needs-test): the most-default reflection scenario, a
    // [UsePaging] [BatchResolver] returning plain List<List<Product>> with no PagingArguments
    // parameter. The cursor paging provider slices each parent's list per context with that
    // context's published first/after, and aliases with different paging arguments are split
    // into separate batch invocations.
    [Fact]
    public async Task UsePaging_Should_Partition_PerAlias_When_PlainListBatchResolverHasDifferentArgs()
    {
        // arrange
        BrandExtensions.BatchCallCount = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    small: products(first: 1) {
                        nodes {
                            name
                        }
                        pageInfo {
                            hasNextPage
                        }
                    }
                    large: products(first: 2) {
                        nodes {
                            name
                        }
                        pageInfo {
                            hasNextPage
                        }
                    }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, BrandExtensions.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 1 P1"
                        }
                      ],
                      "pageInfo": {
                        "hasNextPage": true
                      }
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 1 P1"
                        },
                        {
                          "name": "Brand 1 P2"
                        }
                      ],
                      "pageInfo": {
                        "hasNextPage": true
                      }
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        }
                      ],
                      "pageInfo": {
                        "hasNextPage": true
                      }
                    },
                    "large": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        },
                        {
                          "name": "Brand 2 P2"
                        }
                      ],
                      "pageInfo": {
                        "hasNextPage": true
                      }
                    }
                  }
                ]
              }
            }
            """);
    }

    // PROVES-WORKS: aliases with identical paging arguments coalesce into a single batch
    // invocation (single partition), each context still getting its own per-parent slice.
    [Fact]
    public async Task UsePaging_Should_NotPartition_When_PlainListBatchResolverHasIdenticalArgs()
    {
        // arrange
        BrandExtensions.BatchCallCount = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    small: products(first: 1) {
                        nodes {
                            name
                        }
                    }
                    alsoSmall: products(first: 1) {
                        nodes {
                            name
                        }
                    }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, BrandExtensions.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 1 P1"
                        }
                      ]
                    },
                    "alsoSmall": {
                      "nodes": [
                        {
                          "name": "Brand 1 P1"
                        }
                      ]
                    }
                  },
                  {
                    "name": "Brand 2",
                    "small": {
                      "nodes": [
                        {
                          "name": "Brand 2 P1"
                        }
                      ]
                    },
                    "alsoSmall": {
                      "nodes": [
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
        public static int BatchCallCount { get; set; }

        [UsePaging]
        [BatchResolver]
        public List<List<Product>> GetProducts([Parent] List<Brand> brands)
        {
            BatchCallCount++;
            var result = new List<List<Product>>(brands.Count);

            foreach (var brand in brands)
            {
                result.Add(
                [
                    new Product($"Brand {brand.Id} P1"),
                    new Product($"Brand {brand.Id} P2"),
                    new Product($"Brand {brand.Id} P3")
                ]);
            }

            return result;
        }
    }

    public record Brand(int Id, string Name);

    public record Product(string Name);
}
