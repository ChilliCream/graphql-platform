using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableFilterVisitorListTests
{
    private static readonly Foo[] _fooEntities =
    [
        new()
        {
            FooNested =
            [
                new() { Bar = "a", },
                new() { Bar = "a", },
                new() { Bar = "a", },
            ],
        },
        new()
        {
            FooNested =
            [
                new() { Bar = "c", },
                new() { Bar = "a", },
                new() { Bar = "a", },
            ],
        },
        new()
        {
            FooNested =
            [
                new() { Bar = "a", },
                new() { Bar = "d", },
                new() { Bar = "b", },
            ],
        },
        new()
        {
            FooNested =
            [
                new() { Bar = "c", },
                new() { Bar = "d", },
                new() { Bar = "b", },
            ],
        },
        new()
        {
            FooNested =
            [
                new() { Bar = null!, },
                new() { Bar = "d", },
                new() { Bar = "b", },
            ],
        },
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorListTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_ArrayAllObjectStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { all: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { all: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { all: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "a").AddResult(
                res2,
                "d").AddResult(
                res3,
                "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArraySomeObjectStringEqualWithNull_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"{
                        root(where: {
                            fooNested: {
                                some: {
                                    bar: {
                                        eq: ""a""
                                    }
                                }
                            }
                        }){
                            fooNested {
                                bar
                            }
                        }
                    }")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { some: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { some: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Create());

        // assert
        await Snapshot.Create().AddResult(res1, "a").AddResult(
                res2,
                "d").AddResult(
                res3,
                "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayNoneObjectStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { none: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { none: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { none: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Create());

        // assert
        await Snapshot.Create().AddResult(
                res1,
                "a").AddResult(
                res2,
                "d").AddResult(
                res3,
                "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ArrayAnyObjectStringEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { any: false}}){ fooNested {bar}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { any: true}}){ fooNested {bar}}}")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "false").AddResult(
                res2,
                    "true")
            .MatchAsync();
    }

    public class Foo
    {
        public Guid Id { get; set; }

        public List<FooNested> FooNested { get; set; } = [];
    }

    public class FooSimple
    {
        public List<string> Bar { get; set; } = [];
    }

    public class FooNested
    {
        public int Id { get; set; }

        public string? Bar { get; set; }
    }

    public class FooFilterInput : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.FooNested);
        }
    }

    public class FooSimpleFilterInput : FilterInputType<FooSimple>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FooSimple> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
