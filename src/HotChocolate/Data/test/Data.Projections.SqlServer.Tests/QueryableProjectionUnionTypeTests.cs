using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static HotChocolate.Data.Projections.ProjectionVisitorTestBase;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionUnionTypeTests
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
        public async Task Create_Union()
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
        public async Task Create_Union_Pagination()
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
                                    y.Field(z => z.Root).UsePaging<UnionType<AbstractType>>();
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
        public async Task Create_Union_Nested()
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
        public async Task Create_Union_NestedList()
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
                .AddType(new ObjectType<Foo>())
                .AddType(new ObjectType<Bar>());
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

            public string Name { get; set; }

            public string FooProp { get; set; }
        }

        [UnionType]
        public abstract class AbstractType
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class Bar : AbstractType
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string BarProp { get; set; }
        }
    }
}
