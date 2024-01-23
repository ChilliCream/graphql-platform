using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Raven;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableProjectionNestedTests
{
    private static readonly Bar[] _barEntities =
    [
        new() { Foo = new Foo { BarString = "testatest", }, },
        new() { Foo = new Foo { BarString = "testbtest", }, },
    ];

    private readonly SchemaCache _cache;

    public QueryableProjectionNestedTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_Object()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                        {
                            root {
                                foo {
                                    barString
                                }
                            }
                        }")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNotSettable()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                        {
                            root {
                                notSettable {
                                    barString
                                }
                            }
                        }")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNotSettableList()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                        {
                            root {
                                notSettableList {
                                    barString
                                }
                            }
                        }")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectMethod()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                        {
                            root {
                                method {
                                    barString
                                }
                            }
                        }")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectMethodList()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                        {
                            root {
                                methodList {
                                    barString
                                }
                            }
                        }")
                .Create());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    public class Foo
    {
        public string? Id { get; set; }

        public string BarString { get; set; } = string.Empty;
    }

    public class Bar
    {
        public string? Id { get; set; }

        public Foo Foo { get; set; } = default!;

        public Foo NotSettable { get; } = new() { BarString = "Worked", };

        public Foo Method() => new() { BarString = "Worked", };

        public Foo[] NotSettableList { get; } = [new() { BarString = "Worked", },];

        public Foo[] MethodList() => [new Foo { BarString = "Worked", },];
    }
}
