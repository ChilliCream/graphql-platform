using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionHashSetTests
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
                NestedObject =
                    new BarDeep { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                ObjectSet = new HashSet<BarDeep>
                {
                    new() { Foo = new FooDeep { BarShort = 12, BarString = "a", }, },
                },
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
                NestedObject =
                    new BarDeep { Foo = new FooDeep { BarShort = 12, BarString = "d", }, },
                ObjectSet = new HashSet<BarDeep>
                {
                    new() { Foo = new FooDeep { BarShort = 14, BarString = "d", }, },
                },
            },
        },
    ];

    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Create_DeepFilterObjectTwoProjections()
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
                                    objectSet {
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
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ListObjectDifferentLevelProjection()
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
                                    objectSet {
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
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    private static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Foo>().HasMany(x => x.ObjectSet);
        modelBuilder.Entity<Foo>().HasOne(x => x.NestedObject);
        modelBuilder.Entity<Bar>().HasOne(x => x.Foo);
    }

    public class Foo
    {
        public int Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        [UseFiltering]
        public HashSet<BarDeep>? ObjectSet { get; set; }

        public BarDeep? NestedObject { get; set; }
    }

    public class FooDeep
    {
        public int Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        [UseFiltering]
        public HashSet<BarNullableDeep?>? ObjectSet { get; set; }

        public BarNullableDeep? NestedObject { get; set; }
    }

    public class Bar
    {
        public int Id { get; set; }

        public Foo Foo { get; set; } = default!;
    }

    public class BarDeep
    {
        public int Id { get; set; }

        public FooDeep Foo { get; set; } = default!;
    }

    public class BarNullableDeep
    {
        public int Id { get; set; }

        public FooDeep? Foo { get; set; }
    }

    public class BarNullable
    {
        public int Id { get; set; }

        public FooNullable? Foo { get; set; }
    }

    public enum BarEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX,
    }
}
