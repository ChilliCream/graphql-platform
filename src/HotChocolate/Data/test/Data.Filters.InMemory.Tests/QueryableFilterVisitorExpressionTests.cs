using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorExpressionTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] s_fooEntities =
    [
        new() { Name = "Foo", LastName = "Galoo", Bars = new[] { new Bar { Value="A" } } },
        new() { Name = "Sam", LastName = "Sampleman", Bars = Array.Empty<Bar>() }
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorExpressionTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_StringConcatExpression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInputType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
            .SetDocument("{ root(where: { displayName: { eq: \"Sam Sampleman\"}}){ name lastName}}")
            .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
            .SetDocument("{ root(where: { displayName: { eq: \"NoMatch\"}}){ name lastName}}")
            .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
            .SetDocument("{ root(where: { displayName: { eq: null}}){ name lastName}}")
            .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .Add(res1, "Sam_Sampleman")
            .Add(res2, "NoMatch")
            .Add(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Expression_WithMoreThanOneParameter_ThrowsException()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("Foo")
                .Resolve(Array.Empty<Foo>())
                .UseFiltering())
            .AddType(new FilterInputType<Foo>(x => x
                .Field(f => f.LastName)
                .Extend()
                .OnBeforeCreate(f => f.Expression = (Foo foo, string bar) => foo.LastName == bar)))
            .AddFiltering();

        // act
        async Task<IRequestExecutor> Call() => await builder.BuildRequestExecutorAsync();

        // assert
        var ex = await Assert.ThrowsAsync<SchemaException>(Call);
        ex.Errors.Single().Message.MatchSnapshot();
    }

    [Fact]
    public async Task Create_CollectionExpression_WithVariables()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInputType>(s_fooEntities);
        const string query =
            """
            query Test($where: FooFilterInput) {
              root(where: $where) {
                name
                lastName
              }
            }
            """;

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    { "where", CreateLatestBarFilter("A") }
                })
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    { "where", CreateLatestBarFilter("NoMatch") }
                })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .Add(res1, "A")
            .Add(res2, "NoMatch")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_CollectionLengthExpression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInputType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
            .SetDocument("{ root(where: { barLength: { eq: 1}}){ name lastName}}")
            .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
            .SetDocument("{ root(where: { barLength: { eq: 0}}){ name lastName}}")
            .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
            .SetDocument("{ root(where: { barLength: { eq: null}}){ name lastName}}")
            .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .Add(res1, "1")
            .Add(res2, "0")
            .Add(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    private static Dictionary<string, object?> CreateLatestBarFilter(string value)
        => new()
        {
            {
                "latestBar",
                new Dictionary<string, object?>
                {
                    {
                        "some",
                        new Dictionary<string, object?>
                        {
                            {
                                "value",
                                new Dictionary<string, object?>
                                {
                                    { "eq", value }
                                }
                            }
                        }
                    }
                }
            }
        };

    public class Foo
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? LastName { get; set; }

        public ICollection<Bar>? Bars { get; set; }
    }

    public class Bar
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }

    public class FooFilterInputType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Name + " " + x.LastName).Name("displayName");
            descriptor.Field(x => x.Bars!.Count).Name("barLength");
            descriptor
                .Field(x => x.Bars!.OrderByDescending(b => b.Id).Take(1))
                .Name("latestBar")
                .Type<ListFilterInputType<BarFilterInputType>>();
        }
    }

    public class BarFilterInputType : FilterInputType<Bar>;
}
