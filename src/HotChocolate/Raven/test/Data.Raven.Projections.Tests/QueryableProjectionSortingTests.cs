using CookieCrumble;
using HotChocolate.Execution;

namespace HotChocolate.Data.Raven;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableProjectionSortingTests
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
                NestedObject = new BarDeep { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                ObjectArray =
                [
                    new() { Foo = new FooDeep { BarShort = 1, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 3, BarString = "a", }, },
                ],
            },
        },
        new()
        {
            Foo = new Foo
            {
                BarShort = 14,
                BarBool = true,
                BarEnum = BarEnum.BAZ,
                BarString = "testbtest",
                NestedObject = new BarDeep { Foo = new FooDeep { BarShort = 12, BarString = "d", }, },
                ObjectArray =
                [
                    new() { Foo = new FooDeep { BarShort = 1, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 3, BarString = "a", }, },
                ],
            },
        },
    ];

    private static readonly BarNullable[] _barNullableEntities =
    [
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                NestedObject = new BarDeepNullable { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                ObjectArray =
                [
                    new() { Foo = new FooDeep { BarShort = 1, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 3, BarString = "a", }, },
                ],
            },
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = null,
                BarBool = null,
                BarEnum = BarEnum.BAZ,
                BarString = "testbtest",
                NestedObject = new BarDeepNullable
                {
                    Foo = new FooDeep { BarShort = 12, BarString = "a", },
                },
                ObjectArray =
                [
                    new() { Foo = new FooDeep { BarShort = 1, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 3, BarString = "a", }, },
                ],
            },
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 14,
                BarBool = false,
                BarEnum = BarEnum.QUX,
                BarString = "testctest",
                NestedObject = new BarDeepNullable
                {
                    Foo = new FooDeep { BarShort = 12, BarString = "a", },
                },
                ObjectArray =
                [
                    new() { Foo = new FooDeep { BarShort = 1, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                    new() { Foo = new FooDeep { BarShort = 3, BarString = "a", }, },
                ],
            },
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 13,
                BarBool = false,
                BarEnum = BarEnum.FOO,
                BarString = "testdtest",
                ObjectArray = null!,
            },
        },
    ];

    private readonly SchemaCache _cache;

    public QueryableProjectionSortingTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_DeepFilterObjectTwoProjections()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                foo {
                                    objectArray(
                                        order: {
                                            foo: {
                                                barShort: ASC
                                            }
                                        }) {
                                        foo {
                                            barString
                                            barShort
                                        }
                                    }
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ListObjectDifferentLevelProjection()
    {
        // arrange
        var tester = _cache.CreateSchema(_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                foo {
                                    barString
                                    objectArray(
                                        order: {
                                            foo: {
                                                barShort: ASC
                                            }
                                        }) {
                                        foo {
                                            barString
                                            barShort
                                        }
                                    }
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_DeepFilterObjectTwoProjections_Nullable()
    {
        // arrange
        var tester = _cache.CreateSchema(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                foo {
                                    objectArray(
                                        order: {
                                            foo: {
                                                barShort: ASC
                                            }
                                        }) {
                                        foo {
                                            barString
                                            barShort
                                        }
                                    }
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ListObjectDifferentLevelProjection_Nullable()
    {
        // arrange
        var tester = _cache.CreateSchema(_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        {
                            root {
                                foo {
                                    barString
                                    objectArray(
                                        order: {
                                            foo: {
                                                barShort: ASC
                                            }
                                        }) {
                                        foo {
                                            barString
                                            barShort
                                        }
                                    }
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(res1)
            .MatchAsync();
    }

    public class Foo
    {
        public string? Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        [UseFiltering]
        [UseSorting]
        public List<BarDeep> ObjectArray { get; set; } = default!;

        public BarDeep NestedObject { get; set; } = default!;
    }

    public class FooDeep
    {
        public string? Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;
    }

    public class FooNullable
    {
        public string? Id { get; set; }

        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        public List<BarDeepNullable?> ObjectArray { get; set; } = default!;

        public BarDeepNullable? NestedObject { get; set; }
    }

    public class Bar
    {
        public string? Id { get; set; }

        public Foo Foo { get; set; } = default!;
    }

    public class BarDeep
    {
        public string? Id { get; set; }

        public FooDeep Foo { get; set; } = default!;
    }

    public class BarDeepNullable
    {
        public string? Id { get; set; }

        public FooDeep Foo { get; set; } = default!;
    }

    public class BarNullable
    {
        public string? Id { get; set; }

        public FooNullable? Foo { get; set; }
    }

    public class FooDeepNullable
    {
        public string? Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;
    }

    public enum BarEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX,
    }
}
