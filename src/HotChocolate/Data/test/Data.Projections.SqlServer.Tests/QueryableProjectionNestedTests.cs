using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionNestedTests
{
    private static readonly Bar[] _barEntities =
    [
        new() { Foo = new Foo { BarString = "testatest", }, },
        new() { Foo = new Foo { BarString = "testbtest", }, },
    ];

    private readonly SchemaCache _cache = new SchemaCache();

    [Fact]
    public async Task Create_Object()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities, OnModelCreating);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                foo {
                                    barString
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNotSettable()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities, OnModelCreating);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                notSettable {
                                    barString
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNotSettableList()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities, OnModelCreating);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                notSettableList {
                                    barString
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectMethod()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities, OnModelCreating);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                method {
                                    barString
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectMethodList()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities, OnModelCreating);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                methodList {
                                    barString
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    private static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bar>().HasOne(x => x.Foo);
        modelBuilder.Entity<Bar>().Ignore(x => x.NotSettable);
        modelBuilder.Entity<Bar>().Ignore(x => x.NotSettableList);
    }

    public class Foo
    {
        public int Id { get; set; }

        public string BarString { get; set; } = string.Empty;
    }

    public class Bar
    {
        public int Id { get; set; }

        public Foo Foo { get; set; } = default!;

        public Foo NotSettable { get; } = new() { BarString = "Worked", };

        public Foo Method() => new() { BarString = "Worked", };

        public Foo[] NotSettableList { get; } = [new() { BarString = "Worked", },];

        public Foo[] MethodList() => [new Foo { BarString = "Worked", },];
    }
}
