using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Projections;

public class ProjectionBatchResolverInteractionTests
{
    // REPRO (known break, issue: default filter projection optimizer hijacks a batch child of a
    // projected parent). AddProjections() registers QueryableFilterProjectionOptimizer, whose
    // CanHandle matches any field with a FilterFeature. It calls SetResolver with a null batch
    // pipeline, which wipes the field's BatchResolverPipeline and re-infers the strategy to
    // Default, so the batch resolver never runs and the field silently resolves null. A default
    // user expects the batch child to still execute. This fails today (counter 0, products null).
    [Fact]
    public async Task BatchResolver_Should_Execute_When_ParentUsesProjection_And_FieldUsesFiltering()
    {
        // arrange
        FilterBrandExtensions.BatchCallCount = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FilterQuery>()
            .AddTypeExtension<FilterBrandExtensions>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    products {
                        name
                    }
                }
            }
            """);

        // assert
        Assert.Equal(1, FilterBrandExtensions.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "products": [
                      {
                        "name": "Brand 1 P1"
                      },
                      {
                        "name": "Brand 1 P2"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "products": [
                      {
                        "name": "Brand 2 P1"
                      },
                      {
                        "name": "Brand 2 P2"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    // REPRO (known break, same root cause via the sort projection optimizer).
    // QueryableSortProjectionOptimizer.CanHandle matches any field with a SortingFeature and wipes
    // the batch pipeline the same way. A default user expects the batch child to still execute.
    [Fact]
    public async Task BatchResolver_Should_Execute_When_ParentUsesProjection_And_FieldUsesSorting()
    {
        // arrange
        SortBrandExtensions.BatchCallCount = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<SortQuery>()
            .AddTypeExtension<SortBrandExtensions>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    products {
                        name
                    }
                }
            }
            """);

        // assert
        Assert.Equal(1, SortBrandExtensions.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "name": "Brand 1",
                    "products": [
                      {
                        "name": "Brand 1 P1"
                      },
                      {
                        "name": "Brand 1 P2"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "products": [
                      {
                        "name": "Brand 2 P1"
                      },
                      {
                        "name": "Brand 2 P2"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    public class FilterQuery
    {
        [UseProjection]
        public IQueryable<Brand> GetBrands()
            => new[]
            {
                new Brand(1, "Brand 1"),
                new Brand(2, "Brand 2")
            }.AsQueryable();
    }

    public class SortQuery
    {
        [UseProjection]
        public IQueryable<Brand> GetBrands()
            => new[]
            {
                new Brand(1, "Brand 1"),
                new Brand(2, "Brand 2")
            }.AsQueryable();
    }

    [ExtendObjectType<Brand>]
    public class FilterBrandExtensions
    {
        public static int BatchCallCount { get; set; }

        [UseFiltering]
        [BatchResolver]
        public List<Product[]> GetProducts([Parent] List<Brand> brands)
        {
            BatchCallCount++;
            var result = new List<Product[]>(brands.Count);

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

    [ExtendObjectType<Brand>]
    public class SortBrandExtensions
    {
        public static int BatchCallCount { get; set; }

        [UseSorting]
        [BatchResolver]
        public List<Product[]> GetProducts([Parent] List<Brand> brands)
        {
            BatchCallCount++;
            var result = new List<Product[]>(brands.Count);

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
