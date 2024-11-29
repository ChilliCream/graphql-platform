using HotChocolate.Execution;

namespace HotChocolate.Data.Raven;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableProjectionVisitorIsProjectedTests
{
    private static readonly Foo[] _fooEntities =
    [
        new() { IsProjectedTrue = true, IsProjectedFalse = false, },
        new() { IsProjectedTrue = true, IsProjectedFalse = false, },
    ];

    private static readonly MultipleFoo[] _fooMultipleEntities =
    [
        new() { IsProjectedTrue1 = true, IsProjectedFalse = false, },
        new() { IsProjectedTrue1 = true, IsProjectedFalse = false, },
    ];

    private static readonly Bar[] _barEntities =
    [
        new() { IsProjectedFalse = false, },
        new() { IsProjectedFalse = false, },
    ];

    private readonly SchemaCache _cache;

    public QueryableProjectionVisitorIsProjectedTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task IsProjected_Should_NotBeProjectedWhenSelected_When_FalseWithOneProps()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { isProjectedFalse }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task IsProjected_Should_NotBeProjectedWhenSelected_When_FalseWithTwoProps()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { isProjectedFalse isProjectedTrue  }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task IsProjected_Should_AlwaysBeProjectedWhenSelected_When_True()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { isProjectedFalse }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task IsProjected_Should_AlwaysBeProjectedWhenSelected_When_TrueAndMultiple()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooMultipleEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { isProjectedFalse }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task IsProjected_Should_NotFailWhenSelectionSetSkippedCompletely()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root { isProjectedFalse }}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    public class Foo
    {
        public string? Id { get; set; }

        [IsProjected(true)]
        public bool? IsProjectedTrue { get; set; }

        [IsProjected(false)]
        public bool? IsProjectedFalse { get; set; }

        public bool? ShouldNeverBeProjected { get; set; }
    }

    public class Bar
    {
        public string? Id { get; set; }

        [IsProjected(false)]
        public bool? IsProjectedFalse { get; set; }

        public bool? ShouldNeverBeProjected { get; set; }
    }

    public class MultipleFoo
    {
        public string? Id { get; set; }

        [IsProjected(true)]
        public bool? IsProjectedTrue1 { get; set; }

        [IsProjected(true)]
        public bool? IsProjectedTrue2 { get; set; }

        [IsProjected(true)]
        public bool? IsProjectedTrue3 { get; set; }

        [IsProjected(false)]
        public bool? IsProjectedFalse { get; set; }

        public bool? ShouldNeverBeProjected { get; set; }
    }
}
