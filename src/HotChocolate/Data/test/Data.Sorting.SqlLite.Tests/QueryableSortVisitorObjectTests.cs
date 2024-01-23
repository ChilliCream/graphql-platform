using System.Collections.Generic;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Sorting.Expressions;

public class QueryableSortVisitorObjectTests : IClassFixture<SchemaCache>
{
    private static readonly Bar[] _barEntities =
    [
        new()
        {
            Foo = new Foo
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                //ScalarArray = new[] { "c", "d", "a" },
                ObjectArray = new List<Bar>
                {
                    new()
                    {
                        Foo = new Foo
                        {
                            // ScalarArray = new[] { "c", "d", "a" }
                            BarShort = 12, BarString = "a"
                        }
                    }
                }
            }
        },
        new()
        {
            Foo = new Foo
            {
                BarShort = 14,
                BarBool = true,
                BarEnum = BarEnum.BAZ,
                BarString = "testbtest",
                //ScalarArray = new[] { "c", "d", "b" },
                ObjectArray = new List<Bar>
                {
                    new()
                    {
                        Foo = new Foo
                        {
                            //ScalarArray = new[] { "c", "d", "b" }
                            BarShort = 14, BarString = "d"
                        }
                    }
                }
            }
        },
        new()
        {
            Foo = new Foo
            {
                BarShort = 13,
                BarBool = false,
                BarEnum = BarEnum.FOO,
                BarString = "testctest",
                //ScalarArray = null,
                ObjectArray = null,
            },
        },
    ];

    private static readonly BarNullable?[] _barNullableEntities =
    [
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                //ScalarArray = new[] { "c", "d", "a" },
                ObjectArray = new List<BarNullable>
                {
                    new()
                    {
                        Foo = new FooNullable
                        {
                            //ScalarArray = new[] { "c", "d", "a" }
                            BarShort = 12,
                        }
                    }
                }
            }
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = null,
                BarBool = null,
                BarEnum = BarEnum.BAZ,
                BarString = "testbtest",
                //ScalarArray = new[] { "c", "d", "b" },
                ObjectArray = new List<BarNullable>
                {
                    new()
                    {
                        Foo = new FooNullable
                        {
                            //ScalarArray = new[] { "c", "d", "b" }
                            BarShort = null,
                        }
                    }
                }
            }
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 14,
                BarBool = false,
                BarEnum = BarEnum.QUX,
                BarString = "testctest",
                //ScalarArray = null,
                ObjectArray = new List<BarNullable>
                {
                    new()
                    {
                        Foo = new FooNullable
                        {
                            //ScalarArray = new[] { "c", "d", "b" }
                            BarShort = 14,
                        }
                    }
                }
            }
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 13,
                BarBool = false,
                BarEnum = BarEnum.FOO,
                BarString = "testdtest",
                //ScalarArray = null,
                ObjectArray = null
            }
        },
        new() { Foo = null, },
    ];

    private readonly SchemaCache _cache;

    public QueryableSortVisitorObjectTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_ObjectShort_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barShort: ASC}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barShort: DESC}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShort_OrderBy()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barShort: ASC}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barShort: DESC}}) " +
                    "{ foo{ barShort}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnum_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barEnum: ASC}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barEnum: DESC}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnum_OrderBy()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barEnum: ASC}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barEnum: DESC}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectString_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barString: ASC}}) " +
                    "{ foo{ barString}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barString: DESC}}) " +
                    "{ foo{ barString}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableString_OrderBy()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barString: ASC}}) " +
                    "{ foo{ barString}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barString: DESC}}) " +
                    "{ foo{ barString}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectBool_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barBool: ASC}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barBool: DESC}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableBool_OrderBy()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barBool: ASC}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barBool: DESC}}) " +
                    "{ foo{ barBool}}}")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "ASC"), res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectString_OrderBy_TwoProperties()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barBool: ASC, barShort: ASC }}) " +
                    "{ foo{ barBool barShort}}}")
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                        {
                            root(order: [
                                { foo: { barBool: ASC } },
                                { foo: { barShort: ASC } }]) {
                                foo {
                                    barBool
                                    barShort
                                }
                            }
                        }
                        ")
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(order: { foo: { barBool: DESC, barShort: DESC}}) " +
                    "{ foo{ barBool barShort}}}")
                .Create());

        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                        {
                            root(order: [
                                { foo: { barBool: DESC } },
                                { foo: { barShort: DESC } }]) {
                                foo {
                                    barBool
                                    barShort
                                }
                            }
                        }
                        ")
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "ASC"), res2, "ASC"), res3, "DESC"), res4, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectString_OrderBy_TwoProperties_Variables()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                         query testSort($order: [BarSortInput!]) {
                            root(order: $order) {
                                foo {
                                    barBool
                                    barShort
                                }
                            }
                        }")
                .SetVariableValue(
                    "order",
                    new List<Dictionary<string, object>>
                    {
                            new()
                            {
                                {
                                    "foo",
                                    new Dictionary<string,object>
                                    {
                                         { "barShort", "ASC" },{ "barBool", "ASC" }
                                    }
                                }
                            }
                    })
                .Create());

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                         query testSort($order: [BarSortInput!]) {
                            root(order: $order) {
                                foo {
                                    barBool
                                    barShort
                                }
                            }
                        }")
                .SetVariableValue(
                    "order",
                    new List<Dictionary<string, object>>
                    {
                            new()
                            {
                                {
                                    "foo",
                                    new Dictionary<string,object> { { "barShort", "ASC" }, }
                                },
                            },
                            new()
                            {
                                {
                                    "foo",
                                    new Dictionary<string,object> { { "barBool", "ASC" }, }
                                },
                            },
                    })
                .Create());

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                         query testSort($order: [BarSortInput!]) {
                            root(order: $order) {
                                foo {
                                    barBool
                                    barShort
                                }
                            }
                        }")
                .SetVariableValue(
                    "order",
                    new List<Dictionary<string, object>>
                    {
                            new()
                            {
                                {
                                    "foo",
                                    new Dictionary<string,object>
                                    {
                                        { "barShort", "DESC" },{ "barBool", "DESC" }
                                    }
                                }
                            }
                    })
                .Create());

        var res4 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(
                    @"
                         query testSort($order: [BarSortInput!]) {
                            root(order: $order) {
                                foo {
                                    barBool
                                    barShort
                                }
                            }
                        }")
                .SetVariableValue(
                    "order",
                    new List<Dictionary<string, object>>
                    {
                            new()
                            {
                                {
                                    "foo",
                                    new Dictionary<string, object> { { "barShort", "DESC" }, }
                                },
                            },
                            new()
                            {
                                {
                                    "foo",
                                    new Dictionary<string, object> { { "barBool", "DESC" }, }
                                },
                            },
                    })
                .Create());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        SnapshotExtensions.AddResult(
                            Snapshot
                                .Create(), res1, "ASC"), res2, "ASC"), res3, "DESC"), res4, "DESC")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = "";

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        //Not supported in SQL
        //public string[] ScalarArray { get; set; }

        public List<Bar>? ObjectArray { get; set; } = [];
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        //Not supported in SQL
        //public string?[] ScalarArray { get; set; }

        public List<BarNullable>? ObjectArray { get; set; }
    }

    public class Bar
    {
        public int Id { get; set; }

        public Foo Foo { get; set; } = null!;
    }

    public class BarNullable
    {
        public int Id { get; set; }

        public FooNullable? Foo { get; set; }
    }

    public class BarSortType
        : SortInputType<Bar>
    {
    }

    public class BarNullableSortType
        : SortInputType<BarNullable>
    {
    }

    public enum BarEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX
    }
}
