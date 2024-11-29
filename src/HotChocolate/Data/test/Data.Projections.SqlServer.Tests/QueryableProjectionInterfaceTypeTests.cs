using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using static HotChocolate.Data.Projections.ProjectionVisitorTestBase;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionInterfaceTypeTests
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

    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Create_Interface()
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
                                name
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
    public async Task Create_Interface_Pagination()
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
                                y.Field(z => z.Root).UsePaging<InterfaceType<AbstractType>>();
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
                                    name
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
    public async Task Create_Interface_Nested()
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
                                    name
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
    public async Task Create_Interface_NestedList()
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
                                    name
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
    public async Task Paging_Interface_List()
    {
        // arrange
        var tester = _cache
            .CreateSchema(
                _barEntities,
                OnModelCreating,
                configure: ConfigureSchema,
                schemaType: typeof(InterfaceType<AbstractType>),
                usePaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root {
                                nodes {
                                    name
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
    public async Task OffsetPaging_Interface_List()
    {
        // arrange
        var tester = _cache
            .CreateSchema(
                _barEntities,
                OnModelCreating,
                configure: ConfigureSchema,
                schemaType: typeof(InterfaceType<AbstractType>),
                useOffsetPaging: true);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root {
                                items {
                                    name
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
    public async Task Create_Interface_Without_Missing()
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
            .AddType(new ObjectType<Foo>(x => x.Implements<InterfaceType<AbstractType>>()))
            .AddType(new ObjectType<Bar>(x => x.Implements<InterfaceType<AbstractType>>()));
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
        public new int Id { get; set; }

        public string FooProp { get; set; } = default!;
    }

    [InterfaceType]
    public abstract class AbstractType
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    public class Bar : AbstractType
    {
        public new int Id { get; set; }

        public string? BarProp { get; set; }
    }
}
