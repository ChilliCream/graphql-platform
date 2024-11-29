using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using static HotChocolate.Data.Projections.ProjectionVisitorTestBase;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionUnionTypeTests
{
    private static readonly AbstractType[] _barEntities =
    [
        new Bar { Name = "Bar", BarProp = "BarProp", },
        new Foo { Name = "Foo", FooProp = "FooProp", },
    ];

    private static readonly NestedObject[] _barNestedEntities =
    [
        new() { Nested = new Bar { Name = "Bar", BarProp = "BarProp", }, },
        new() { Nested = new Foo { Name = "Foo", FooProp = "FooProp", }, },
    ];

    private static readonly NestedList[] _barListEntities =
    [
        new()
        {
            List =
            [
                new Foo { Name = "Foo", FooProp = "FooProp", },
                new Bar { Name = "Bar", BarProp = "BarProp", },
            ],
        },
        new()
        {
            List =
            [
                new Bar { Name = "Bar", BarProp = "BarProp", },
                new Foo { Name = "Foo", FooProp = "FooProp", },
            ],
        },
    ];

    private readonly SchemaCache _cache = new SchemaCache();

    [Fact]
    public async Task Create_Union()
    {
        // arrange
        var tester =
            _cache.CreateSchema(_barEntities, OnModelCreating, configure: ConfigureSchema);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root {
                                ... on Foo {
                                    fooProp
                                }
                                ... on Bar {
                                    barProp
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
    public async Task Create_Union_Pagination()
    {
        // arrange
        var tester =
            _cache.CreateSchema(_barEntities,
                OnModelCreating,
                configure: x =>
                {
                    ConfigureSchema(x);

                    var typeExtension =
                        new ObjectTypeExtension<StubObject<AbstractType>>(
                            y =>
                            {
                                y.Name("Query");
                                y.Field(z => z.Root).UsePaging<UnionType<AbstractType>>();
                            });

                    x.AddType(typeExtension);
                });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root {
                                nodes {
                                    ... on Foo {
                                        fooProp
                                    }
                                    ... on Bar {
                                        barProp
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
    public async Task Create_Union_Nested()
    {
        // arrange
        var tester = _cache
            .CreateSchema(_barNestedEntities, OnModelCreating, configure: ConfigureSchema);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root {
                                nested {
                                    ... on Foo {
                                        fooProp
                                    }
                                    ... on Bar {
                                        barProp
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
    public async Task Create_Union_NestedList()
    {
        // arrange
        var tester = _cache
            .CreateSchema(_barListEntities, OnModelCreating, configure: ConfigureSchema);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root {
                                list {
                                    ... on Foo {
                                        fooProp
                                    }
                                    ... on Bar {
                                        barProp
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
    public async Task Create_Union_Without_Missing()
    {
        // arrange
        var tester =
            _cache.CreateSchema(_barEntities, OnModelCreating, configure: ConfigureSchema);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root {
                                ... on Foo {
                                    fooProp
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
        modelBuilder.Entity<AbstractType>()
            .HasDiscriminator<string>("d")
            .HasValue<Bar>("bar")
            .HasValue<Foo>("foo");
    }

    private static void ConfigureSchema(ISchemaBuilder schemaBuilder)
    {
        schemaBuilder
            .AddType(new ObjectType<Foo>())
            .AddType(new ObjectType<Bar>());
    }

    public class NestedList
    {
        public int Id { get; set; }

        public List<AbstractType> List { get; set; } = default!;
    }

    public class NestedObject
    {
        public int Id { get; set; }

        public AbstractType Nested { get; set; } = default!;
    }

    public class Foo : AbstractType
    {
        public string FooProp { get; set; } = default!;
    }

    [UnionType]
    public abstract class AbstractType
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    public class Bar : AbstractType
    {
        public string BarProp { get; set; } = default!;
    }
}
