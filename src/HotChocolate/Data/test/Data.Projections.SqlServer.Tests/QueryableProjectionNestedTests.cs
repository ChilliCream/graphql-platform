using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionNestedTests
    {
        private static readonly Bar[] _barEntities =
        {
            new() { Foo = new Foo { BarString = "testatest", } },
            new() { Foo = new Foo { BarString = "testbtest", } }
        };

        private readonly SchemaCache _cache = new SchemaCache();

        [Fact]
        public async Task Create_Object()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        {
                            root {
                                foo {
                                    barString
                                }
                            }
                        }")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ObjectNotSettable()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        {
                            root {
                                notSettable {
                                    barString
                                }
                            }
                        }")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ObjectNotSettableList()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        {
                            root {
                                notSettableList {
                                    barString
                                }
                            }
                        }")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ObjectMethod()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        {
                            root {
                                method {
                                    barString
                                }
                            }
                        }")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ObjectMethodList()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        {
                            root {
                                methodList {
                                    barString
                                }
                            }
                        }")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        public static void OnModelCreating(ModelBuilder modelBuilder)
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

            public Foo Foo { get; set; }

            public Foo NotSettable { get; } = new() { BarString = "Worked" };

            public Foo Method() => new() { BarString = "Worked" };

            public Foo[] NotSettableList { get; } =
            {
                new() { BarString = "Worked" }
            };

            public Foo[] MethodList() => new[]
            {
                new Foo() { BarString = "Worked" }
            };
        }
    }
}
