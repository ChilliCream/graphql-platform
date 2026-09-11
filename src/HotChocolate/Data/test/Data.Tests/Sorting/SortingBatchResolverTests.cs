using CookieCrumble;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class SortingBatchResolverTests
{
    [Fact]
    public async Task UseSorting_Should_IsolateEntries_When_PostSortingActionFails()
    {
        // arrange
        var batches = new List<int[]>();
        var applied = new List<string>();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType(new ObjectType<Brand>(d => d.Field("products")
                .Type<ListType<ObjectType<Product>>>()
                .UseSorting<Product>()
                .ResolveBatch(contexts =>
                {
                    batches.Add(contexts.Select(c => c.Parent<Brand>().Id).ToArray());
                    var results = new ResolverResult[contexts.Count];

                    for (var i = 0; i < contexts.Count; i++)
                    {
                        var context = contexts[i];
                        var id = context.Parent<Brand>().Id;
                        context.SetLocalState<PostSortingAction<IQueryable<Product>>>(
                            QueryableSortProvider.PostSortingActionKey,
                            (sorted, query) =>
                            {
                                applied.Add($"{id}:{sorted}");

                                if (id == 1)
                                {
                                    throw TestThrowHelper.CannotSortParentProducts();
                                }

                                return query;
                            });
                        results[i] = ResolverResult.Ok(new[] { new Product("P1"), new Product("P2") });
                    }

                    return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                })))
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ brands { products(order: { name: DESC }) { name } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(batches, "Resolver batches")
            .Add(applied, "Per-entry sorting callbacks")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseSorting_Should_UsePerAliasOrder_When_OneParentHandlesSorting(bool handled)
    {
        // arrange
        var batches = new List<int[]>();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType(new ObjectType<Brand>(d => d.Field("products")
                .Type<ListType<ObjectType<Product>>>()
                .UseSorting<Product>()
                .ResolveBatch(contexts =>
                {
                    batches.Add(contexts.Select(c => c.Parent<Brand>().Id).ToArray());
                    var results = new ResolverResult[contexts.Count];

                    for (var i = 0; i < contexts.Count; i++)
                    {
                        if (handled && contexts[i].Parent<Brand>().Id == 1)
                        {
                            contexts[i].GetSortingContext()!.Handled(true);
                        }

                        results[i] = ResolverResult.Ok(new[] { new Product("P1"), new Product("P2") });
                    }

                    return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                })))
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    a: products(order: { name: ASC }) { name }
                    b: products(order: { name: DESC }) { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: handled.ToString())
            .Add(result, "Result")
            .Add(batches, "Resolver batches")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task UseSorting_Should_Order_PerParentResults_When_FieldIsBatchResolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

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
            """,
            cancellationToken: TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

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

file static class TestThrowHelper
{
    public static GraphQLException CannotSortParentProducts()
        => new("Cannot sort this parent's products.");
}
