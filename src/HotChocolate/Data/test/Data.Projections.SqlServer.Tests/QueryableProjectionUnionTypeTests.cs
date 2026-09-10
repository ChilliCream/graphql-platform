using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using static HotChocolate.Data.Projections.ProjectionVisitorTestBase;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionUnionTypeTests
{
    private static readonly AbstractType[] s_barEntities =
    [
        new Bar { Name = "Bar", BarProp = "BarProp" },
        new Foo { Name = "Foo", FooProp = "FooProp" }
    ];

    private static readonly NestedObject[] s_barNestedEntities =
    [
        new() { Nested = new Bar { Name = "Bar", BarProp = "BarProp" } },
        new() { Nested = new Foo { Name = "Foo", FooProp = "FooProp" } }
    ];

    private static readonly NestedList[] s_barListEntities =
    [
        new()
        {
            List =
            [
                new Foo { Name = "Foo", FooProp = "FooProp" },
                new Bar { Name = "Bar", BarProp = "BarProp" }
            ]
        },
        new()
        {
            List =
            [
                new Bar { Name = "Bar", BarProp = "BarProp" },
                new Foo { Name = "Foo", FooProp = "FooProp" }
            ]
        }
    ];

    private static readonly InspectionDefinition[] s_inspectionDefinitions =
    [
        new()
        {
            Id = 1,
            Trigger = new FieldDateTimeInspectionTrigger
            {
                Id = 11,
                FieldModelKey = "field-1"
            }
        },
        new()
        {
            Id = 2,
            Trigger = new FieldDateTimeInspectionTrigger
            {
                Id = 12,
                FieldModelKey = "field-2"
            }
        }
    ];

    private readonly SchemaCache _cache = new SchemaCache();

    [Fact]
    public async Task Create_Union()
    {
        // arrange
        var tester =
            _cache.CreateSchema(s_barEntities, OnModelCreating, configure: ConfigureSchema);

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
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UseProjection_Should_ProjectExplicitUnion_When_ResolverReturnsBaseQueryable()
    {
        // arrange
        var tester = _cache.CreateSchema(
            s_barEntities,
            OnModelCreating,
            configure: builder =>
            {
                ConfigureSchema(builder);
                builder.AddType(
                    new ObjectTypeExtension<StubObject<AbstractType>>(
                        descriptor =>
                        {
                            descriptor.Name("Query");
                            descriptor
                                .Field(x => x.Root)
                                .Type<ListType<ExplicitUnionType>>();
                        }));
            });

        // act
        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        root {
                            __typename
                            ... on Foo {
                                fooProp
                            }
                            ... on Bar {
                                barProp
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectConcreteProperties_When_ReturnTypeIsUnion()
    {
        // arrange
        var tester = _cache.CreateSchema(
            s_barEntities,
            OnModelCreating,
            configure: ConfigureSchemaWithRequirements,
            asNoTracking: true);

        // act
        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        root {
                            __typename
                            ... on Foo {
                                requiredFooProp
                            }
                            ... on Bar {
                                requiredBarProp
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_Union_Pagination()
    {
        // arrange
        var tester =
            _cache.CreateSchema(s_barEntities,
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
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_Union_Nested()
    {
        // arrange
        var tester = _cache
            .CreateSchema(s_barNestedEntities, OnModelCreating, configure: ConfigureSchema);

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
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_Union_NestedList()
    {
        // arrange
        var tester = _cache
            .CreateSchema(s_barListEntities, OnModelCreating, configure: ConfigureSchema);

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
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_Union_Without_Missing()
    {
        // arrange
        var tester =
            _cache.CreateSchema(s_barEntities, OnModelCreating, configure: ConfigureSchema);

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
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_Union_Single_Property()
    {
        // arrange
        var tester = _cache.CreateSchema(
            s_inspectionDefinitions,
            OnModelCreatingInspection,
            configure: ConfigureInspectionSchema);

        // act
        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        root {
                            trigger {
                                ... on FieldDateTimeInspectionTrigger {
                                    fieldModelKey
                                }
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_Union_Single_Property_Pagination()
    {
        // arrange
        var tester = _cache.CreateSchema(
            s_inspectionDefinitions,
            OnModelCreatingInspection,
            usePaging: true,
            configure: ConfigureInspectionSchema);

        // act
        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        root {
                            nodes {
                                trigger {
                                    ... on FieldDateTimeInspectionTrigger {
                                        fieldModelKey
                                    }
                                }
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
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

    private static void ConfigureSchemaWithRequirements(ISchemaBuilder schemaBuilder)
    {
        schemaBuilder
            .AddType(
                new ObjectType<Foo>(
                    descriptor => descriptor
                        .Field("requiredFooProp")
                        .Type<NonNullType<StringType>>()
                        .ParentRequires<Foo>(foo => foo.FooProp)
                        .Resolve(context => context.Parent<Foo>().FooProp)))
            .AddType(
                new ObjectType<Bar>(
                    descriptor => descriptor
                        .Field("requiredBarProp")
                        .Type<NonNullType<StringType>>()
                        .ParentRequires<Bar>(nameof(Bar.BarProp))
                        .Resolve(context => context.Parent<Bar>().BarProp)));
    }

    private static void OnModelCreatingInspection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InspectionDefinition>()
            .HasOne(x => x.Trigger)
            .WithOne()
            .HasForeignKey<InspectionDefinition>(x => x.TriggerId);

        modelBuilder.Entity<InspectionTrigger>()
            .HasDiscriminator<string>("d")
            .HasValue<FieldDateTimeInspectionTrigger>("fieldDateTime");
    }

    private static void ConfigureInspectionSchema(ISchemaBuilder schemaBuilder)
        => schemaBuilder.AddType(new ObjectType<FieldDateTimeInspectionTrigger>());

    public class NestedList
    {
        public int Id { get; set; }

        public List<AbstractType> List { get; set; } = null!;
    }

    public class NestedObject
    {
        public int Id { get; set; }

        public AbstractType Nested { get; set; } = null!;
    }

    public class Foo : AbstractType
    {
        public string FooProp { get; set; } = null!;
    }

    [UnionType]
    public abstract class AbstractType
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
    }

    public class Bar : AbstractType
    {
        public string BarProp { get; set; } = null!;
    }

    public class ExplicitUnionType : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("ExplicitUnion");
            descriptor.Type<ObjectType<Foo>>();
            descriptor.Type<ObjectType<Bar>>();
        }
    }

    public class InspectionDefinition
    {
        public int Id { get; set; }

        public int TriggerId { get; set; }

        public InspectionTrigger Trigger { get; set; } = null!;
    }

    [UnionType]
    public abstract class InspectionTrigger
    {
        public int Id { get; set; }
    }

    public class FieldDateTimeInspectionTrigger : InspectionTrigger
    {
        public string FieldModelKey { get; set; } = null!;
    }
}
