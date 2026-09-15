using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Projections;

public class ProjectionBatchResolverInteractionTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("fieldError")]
    [InlineData("error")]
    [InlineData("reported")]
    [InlineData("cached")]
    public async Task UseProjection_Should_PreserveEntryResults_When_MiddlewareShortCircuits(string state)
    {
        // arrange
        var batches = new List<int[]>();
        var widths = new List<int>();
        var fieldErrors = new List<bool>();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType(new ObjectType<Brand>(d => d.Field("products")
                .Type<ListType<ObjectType<ProjectedProduct>>>()
                .UseBatch(next => async contexts =>
                {
                    widths.Add(contexts.Length);
                    var context = contexts[0];
                    var error = TestErrorHelper.EntryError();

                    switch (state)
                    {
                        case "null":
                            context.Result = null;
                            break;
                        case "fieldError":
                            context.Result = new FieldResult<ProjectedProduct[]>(error);
                            break;
                        case "error":
                            context.Result = error;
                            break;
                        case "reported":
                            context.ReportError(error);
                            break;
                        case "cached":
                            context.ReportError(error);
                            context.Result = new[]
                            {
                                new ProjectedProduct { Name = "cached-A", Unselected = "secret" },
                                new ProjectedProduct { Name = "cached-B", Unselected = "secret" }
                            }.AsQueryable();
                            break;
                    }

                    await next(contexts);

                    if (state == "fieldError")
                    {
                        var preserved = context.Result is IFieldResult { IsError: true };
                        fieldErrors.Add(preserved);

                        if (preserved)
                        {
                            context.ReportError(error);
                            context.Result = null;
                        }
                    }
                })
                .UseProjection<ProjectedProduct>()
                .UseFiltering<ProjectedProduct>()
                .UseSorting<ProjectedProduct>()
                .ResolveBatch(contexts =>
                {
                    batches.Add(contexts.Select(c => c.Parent<Brand>().Id).ToArray());
                    IReadOnlyList<ResolverResult> results = contexts.Select(c => ResolverResult.Ok(new[]
                    {
                        new ProjectedProduct { Name = $"{c.Parent<Brand>().Id}-A", Unselected = "secret" },
                        new ProjectedProduct { Name = $"{c.Parent<Brand>().Id}-B", Unselected = "secret" }
                    }.AsQueryable())).ToArray();
                    return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                })))
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ brands { products(where: { name: { endsWith: \"B\" } }, order: { name: DESC }) { name } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: state)
            .Add(result, "Result")
            .Add(batches, "Resolver batches")
            .Add(widths, "Middleware widths")
            .Add(fieldErrors, "Field errors preserved for outer middleware")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseProjection_Should_ComposeQuery_When_BatchReturnsQueryable(bool executable)
    {
        // arrange
        var batches = new List<int[]>();
        var expressions = new List<string>();
        var projected = new List<string>();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType(new ObjectType<Brand>(d =>
            {
                d.Field("products")
                    .Type<ListType<ObjectType<ProjectedProduct>>>()
                    .UseBatch(next => async contexts =>
                    {
                        await next(contexts);

                        foreach (var context in contexts)
                        {
                            var query = context.Result switch
                            {
                                IQueryable<ProjectedProduct> q => q,
                                IQueryableExecutable<ProjectedProduct> e => e.Source,
                                _ => null
                            };

                            if (query is not null)
                            {
                                expressions.Add(query.Expression.ToString());
                                projected.AddRange(query.Select(p => $"{p.Name}:{p.Unselected}"));
                            }
                        }
                    })
                    .UseProjection<ProjectedProduct>()
                    .UseFiltering<ProjectedProduct>()
                    .UseSorting<ProjectedProduct>()
                    .ResolveBatch(async contexts =>
                    {
                        await Task.Yield();
                        batches.Add(contexts.Select(c => c.Parent<Brand>().Id).ToArray());
                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            var id = contexts[i].Parent<Brand>().Id;
                            var query = new[]
                            {
                                new ProjectedProduct { Name = $"{id}-A", Unselected = "secret" },
                                new ProjectedProduct { Name = $"{id}-B", Unselected = "secret" },
                                new ProjectedProduct { Name = $"{id}-C", Unselected = "secret" }
                            }.AsQueryable();
                            results[i] = ResolverResult.Ok(executable ? query.AsExecutable() : query);
                        }

                        return results;
                    });
            }))
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                brands {
                    products(where: { or: [{ name: { endsWith: "A" } }, { name: { endsWith: "B" } }] }, order: { name: DESC }) {
                        name
                    }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(batches, "Resolver batches")
            .Add(expressions, "Composed queries")
            .Add(projected, "Projected values before completion")
            .MatchMarkdownSnapshot();
    }

    public class Query
    {
        public Brand[] GetBrands() => [new(1, "Brand 1"), new(2, "Brand 2")];
    }

    public class ProjectedProduct
    {
        public string Name { get; set; } = null!;

        public string? Unselected { get; set; }
    }

    public class Brand
    {
        public Brand() { }

        public Brand(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; set; }

        public string Name { get; set; } = null!;
    }

    public record Product(string Name);
}

file static class TestErrorHelper
{
    public static IError EntryError()
        => ErrorBuilder.New().SetMessage("Entry error.").SetCode("ENTRY").Build();
}
