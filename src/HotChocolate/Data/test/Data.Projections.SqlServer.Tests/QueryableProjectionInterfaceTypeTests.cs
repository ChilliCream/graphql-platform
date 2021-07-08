using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static HotChocolate.Data.Projections.ProjectionVisitorTestBase;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionInterfaceTypeTests
    {
        private static readonly AbstractType[] _barEntities =
        {
            new Bar { Name = "Bar", BarProp = "BarProp" },
            new Foo { Name = "Foo", FooProp = "FooProp" }
        };

        private static readonly NestedObject[] _barNestedEntities =
        {
            new() { Nested = new Bar { Name = "Bar", BarProp = "BarProp" } },
            new() { Nested = new Foo { Name = "Foo", FooProp = "FooProp" } },
        };

        private static readonly NestedList[] _barListEntities =
        {
            new()
            {
                List = new()
                {
                    new Foo { Name = "Foo", FooProp = "FooProp" },
                    new Bar { Name = "Bar", BarProp = "BarProp" }
                }
            },
            new()
            {
                List = new()
                {
                    new Bar { Name = "Bar", BarProp = "BarProp" },
                    new Foo { Name = "Foo", FooProp = "FooProp" }
                }
            },
        };

        private readonly SchemaCache _cache = new SchemaCache();

        [Fact]
        public async Task Create_Interface()
        {
            // arrange
            IRequestExecutor tester =
                _cache.CreateSchema(_barEntities, OnModelCreating, configure: ConfigureSchema);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
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
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_Interface_Pagination()
        {
            // arrange
            IRequestExecutor tester =
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
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
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
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_Interface_Nested()
        {
            // arrange
            IRequestExecutor tester = _cache
                .CreateSchema(_barNestedEntities, OnModelCreating, configure: ConfigureSchema);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
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
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_Interface_NestedList()
        {
            // arrange
            IRequestExecutor tester = _cache
                .CreateSchema(_barListEntities, OnModelCreating, configure: ConfigureSchema);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
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
                    .Create());

            res1.MatchSqlSnapshot();
        }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AbstractType>()
                .HasDiscriminator<string>("d")
                .HasValue<Bar>("bar")
                .HasValue<Foo>("foo");
        }

        public static void ConfigureSchema(ISchemaBuilder schemaBuilder)
        {
            schemaBuilder
                .AddType(new ObjectType<Foo>(
                    x => x.Implements<InterfaceType<AbstractType>>()))
                .AddType(new ObjectType<Bar>(
                    x => x.Implements<InterfaceType<AbstractType>>()));
        }

        public class NestedList
        {
            public int Id { get; set; }

            public List<AbstractType> List { get; set; }
        }

        public class NestedObject
        {
            public int Id { get; set; }

            public AbstractType Nested { get; set; }
        }

        public class Foo : AbstractType
        {
            public int Id { get; set; }

            public string FooProp { get; set; }
        }

        [InterfaceType]
        public abstract class AbstractType
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class Bar : AbstractType
        {
            public int Id { get; set; }

            public string BarProp { get; set; }
        }
    }
}
