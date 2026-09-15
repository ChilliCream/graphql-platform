using CookieCrumble;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class FilteringBatchResolverTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseFiltering_Should_IsolateEntries_When_OneFilterFails(bool fieldResult)
    {
        // arrange
        var batches = new List<int[]>();
        var widths = new List<int>();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType(new ObjectType<Brand>(d => d.Field("products")
                .Type<ListType<ObjectType<Product>>>()
                .UseBatch(next => async contexts =>
                {
                    widths.Add(contexts.Length);
                    await next(contexts);
                })
                .UseFiltering<Product>()
                .ResolveBatch(contexts =>
                {
                    batches.Add(contexts.Select(c => c.Parent<Brand>().Id).ToArray());
                    var results = new ResolverResult[contexts.Count];

                    for (var i = 0; i < contexts.Count; i++)
                    {
                        var context = contexts[i];

                        if (context.Parent<Brand>().Id == 1)
                        {
                            context.SetLocalState(
                                QueryableFilterProvider.ContextValueNodeKey,
                                Utf8GraphQLParser.Syntax.ParseValueLiteral("{ name: { contains: null } }"));
                        }

                        Product[] products = [new("P1"), new("P2")];
                        results[i] = ResolverResult.Ok(fieldResult
                            ? new FieldResult<Product[]>(products)
                            : products);
                    }

                    return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                })))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ brands { products(where: { name: { eq: \"P1\" } }) { name } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(batches, "Resolver batches")
            .Add(widths, "Middleware widths")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task UseFiltering_Should_PreserveHandledState_When_OneEntryHandlesItsPredicate()
    {
        // arrange
        var predicates = new List<bool>();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType(new ObjectType<Brand>(d => d.Field("products")
                .Type<ListType<ObjectType<Product>>>()
                .UseFiltering<Product>()
                .ResolveBatch(contexts =>
                {
                    var results = new ResolverResult[contexts.Count];

                    for (var i = 0; i < contexts.Count; i++)
                    {
                        var filter = contexts[i].GetFilterContext()!;
                        predicates.Add(filter.AsPredicate<Product>() is not null);

                        filter.Handled(contexts[i].Parent<Brand>().Id == 1);

                        results[i] = ResolverResult.Ok(new[] { new Product("P1"), new Product("P2") });
                    }

                    return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                })))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ brands { products(where: { name: { eq: \"P1\" } }) { name } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(predicates, "Predicates available inside resolver")
            .MatchMarkdownSnapshot();
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

    public record Brand(int Id, string Name);

    public record Product(string Name);
}
