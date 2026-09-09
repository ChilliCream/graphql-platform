using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class FilteringBatchResolverTests
{
    // REPRO (known break, issue: filtering middleware is dead on batch fields).
    // [UseFiltering] on a [BatchResolver] field adds the `where` argument but the
    // compiled filter middleware sits in the regular pipeline that batch selections
    // never execute, so the argument is coerced and silently ignored. A default user
    // expects each parent's result list to be filtered. This fails today (both products
    // come back) and is the acceptance test for the fix.
    [Fact]
    public async Task UseFiltering_Should_Filter_PerParentResults_When_FieldIsBatchResolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    products(where: { name: { eq: "P1" } }) {
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
                        "name": "P1"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "products": [
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
    public async Task UseFiltering_Should_Expose_WhereArgument_When_FieldIsBatchResolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        var brandType = executor.Schema.Types.GetType<ObjectType>("Brand");
        var products = brandType.Fields["products"];
        var where = products.Arguments["where"];

        // assert
        Assert.Equal("ProductFilterInput", where.Type.NamedType().Name);
    }

    // REPRO (known break, refutes the "works" finding for the GetFilterContext workaround).
    // GetFilterContext() returns a non-null context with the per-alias `where` literal, but
    // AsPredicate<T>() returns null: the predicate delegate is published into LocalContextData
    // by the filter middleware (QueryableQueryBuilder.Prepare), which never runs on batch fields.
    // A default user expects each alias's predicate to filter its own list. This fails today.
    [Fact]
    public async Task GetFilterContext_Should_Apply_PerAlias_Predicate_When_UsedInsideBatchResolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<PredicateBrandType>()
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    name
                    a: products(where: { name: { eq: "P1" } }) {
                        name
                    }
                    b: products(where: { name: { eq: "P2" } }) {
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
                    "a": [
                      {
                        "name": "P1"
                      }
                    ],
                    "b": [
                      {
                        "name": "P2"
                      }
                    ]
                  },
                  {
                    "name": "Brand 2",
                    "a": [
                      {
                        "name": "P1"
                      }
                    ],
                    "b": [
                      {
                        "name": "P2"
                      }
                    ]
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
        [UseFiltering]
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

    public sealed class PredicateBrandType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor
                .Field("products")
                .Type<ListType<ObjectType<Product>>>()
                .UseFiltering<Product>()
                .ResolveBatch(contexts =>
                {
                    var results = new ResolverResult[contexts.Count];

                    for (var i = 0; i < contexts.Count; i++)
                    {
                        var context = contexts[i];
                        var products = new[] { new Product("P1"), new Product("P2") };
                        var predicate = context.GetFilterContext()?.AsPredicate<Product>();

                        var filtered = predicate is null
                            ? products
                            : products.Where(predicate.Compile()).ToArray();

                        results[i] = ResolverResult.Ok(filtered);
                    }

                    return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                });
        }
    }

    public record Brand(int Id, string Name);

    public record Product(string Name);
}
