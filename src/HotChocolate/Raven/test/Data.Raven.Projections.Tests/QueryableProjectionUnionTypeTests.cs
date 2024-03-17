using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Data.Raven.ProjectionVisitorTestBase;

namespace HotChocolate.Data.Raven;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
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

    private readonly SchemaCache _cache;

    public QueryableProjectionUnionTypeTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact(Skip = "As currently not supported")]
    public async Task Create_Union()
    {
        // arrange
        var tester =
            _cache.CreateSchema(_barEntities, configure: ConfigureSchema);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
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
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact(Skip = "As currently not supported")]
    public async Task Create_Union_Pagination()
    {
        // arrange
        var tester =
            _cache.CreateSchema(_barEntities,
                configure:
                x =>
                {
                    ConfigureSchema(x);

                    var typeExtension = new ObjectTypeExtension<StubObject<AbstractType>>(
                        y =>
                        {
                            y.Name("Query");
                            y.Field(z => z.Root).UsePaging<UnionType<AbstractType>>();
                        });

                    x.AddTypeExtension(typeExtension);
                });

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
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
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact(Skip = "As currently not supported")]
    public async Task Create_Union_Nested()
    {
        // arrange
        var tester = _cache
            .CreateSchema(_barNestedEntities, configure: ConfigureSchema);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
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
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact(Skip = "As currently not supported")]
    public async Task Create_Union_NestedList()
    {
        // arrange
        var tester = _cache
            .CreateSchema(_barListEntities, configure: ConfigureSchema);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
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
            .Create().AddResult(res1)
            .MatchAsync();
    }

    [Fact(Skip = "As currently not supported")]
    public async Task Create_Union_Without_Missing()
    {
        // arrange
        var tester =
            _cache.CreateSchema(_barEntities, configure: ConfigureSchema);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
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
            .Create().AddResult(res1)
            .MatchAsync();
    }

    private static void ConfigureSchema(IRequestExecutorBuilder builder)
    {
        builder
            .AddType(new ObjectType<Foo>())
            .AddType(new ObjectType<Bar>());
    }

    public class NestedList
    {
        public string? Id { get; set; }

        public List<AbstractType> List { get; set; } = default!;
    }

    public class NestedObject
    {
        public string? Id { get; set; }

        public AbstractType Nested { get; set; } = default!;
    }

    public class Foo : AbstractType
    {
        public string FooProp { get; set; } = default!;
    }

    [UnionType]
    public class AbstractType
    {
        public string? Id { get; set; }

        public string Name { get; set; } = default!;
    }

    public class Bar : AbstractType
    {
        public string BarProp { get; set; } = default!;
    }
}
