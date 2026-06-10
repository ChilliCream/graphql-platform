using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionVisitorScalarTests
{
    private static readonly Foo[] s_fooEntities =
    [
        new() { Bar = true, Baz = "a" }, new() { Bar = false, Baz = "b" }
    ];

    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Create_NotSettable_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root{ notSettable }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_Computed_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root{ computed }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ProjectsTwoProperties_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root{ bar baz }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root{ baz }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ProjectsOneProperty_WithResolver()
    {
        // arrange
        var tester = _cache.CreateSchema(
            s_fooEntities,
            objectType: new ObjectType<Foo>(
                x => x
                    .Field("foo")
                    .Resolve(new[] { "foo" })
                    .Type<ListType<StringType>>()));

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root{ baz foo }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    public class Foo
    {
        public int Id { get; set; }

        public bool Bar { get; set; }

        public string? Baz { get; set; }

        public string Computed() => "Foo";

        public string? NotSettable { get; }
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public bool? Bar { get; set; }

        public string? Baz { get; set; }
    }
}
