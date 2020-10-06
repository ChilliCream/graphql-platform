using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionVisitorObjectTests
    {
        private static readonly Bar[] _barEntities =
        {
            new Bar
            {
                Foo = new Foo
                {
                    BarShort = 12,
                    BarBool = true,
                    BarEnum = BarEnum.BAR,
                    BarString = "testatest",
                    NestedObject =
                        new BarDeep { Foo = new FooDeep { BarShort = 12, BarString = "a" } },
                    ObjectArray = new List<BarDeep>
                    {
                        new BarDeep { Foo = new FooDeep { BarShort = 12, BarString = "a" } }
                    }
                }
            },
            new Bar
            {
                Foo = new Foo
                {
                    BarShort = 14,
                    BarBool = true,
                    BarEnum = BarEnum.BAZ,
                    BarString = "testbtest",
                    NestedObject =
                        new BarDeep { Foo = new FooDeep { BarShort = 12, BarString = "d" } },
                    ObjectArray = new List<BarDeep>
                    {
                        new BarDeep { Foo = new FooDeep { BarShort = 14, BarString = "d" } }
                    }
                }
            }
        };

        private static readonly BarNullable[] _barNullableEntities =
        {
            new BarNullable
            {
                Foo = new FooNullable
                {
                    BarShort = 12,
                    BarBool = true,
                    BarEnum = BarEnum.BAR,
                    BarString = "testatest",
                    ObjectArray = new List<BarNullable>
                    {
                        new BarNullable { Foo = new FooNullable { BarShort = 12, } }
                    }
                }
            },
            new BarNullable
            {
                Foo = new FooNullable
                {
                    BarShort = null,
                    BarBool = null,
                    BarEnum = BarEnum.BAZ,
                    BarString = "testbtest",
                    ObjectArray = new List<BarNullable>
                    {
                        new BarNullable
                        {
                            Foo = new FooNullable { BarShort = null, }
                        }
                    }
                }
            },
            new BarNullable
            {
                Foo = new FooNullable
                {
                    BarShort = 14,
                    BarBool = false,
                    BarEnum = BarEnum.QUX,
                    BarString = "testctest",
                    ObjectArray = new List<BarNullable>
                    {
                        new BarNullable { Foo = new FooNullable { BarShort = 14 } }
                    }
                }
            },
            new BarNullable
            {
                Foo = new FooNullable
                {
                    BarShort = 13,
                    BarBool = false,
                    BarEnum = BarEnum.FOO,
                    BarString = "testdtest",
                    ObjectArray = null
                }
            }
        };

        private readonly SchemaCache _cache = new SchemaCache();

        [Fact]
        public async Task Create_ObjectSingleProjection()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { foo{ barShort}}}")
                    .Create());

            res1.MatchSqlSnapshot("12");
        }

        [Fact]
        public async Task Create_ObjectTwoProjections()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { foo{ barString barShort}}}")
                    .Create());

            res1.MatchSqlSnapshot("12");
        }

        [Fact]
        public async Task Create_NestedObjectTwoProjections()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { foo { nestedObject { foo { barString barShort}}}}}")
                    .Create());

            res1.MatchSqlSnapshot("12");
        }

        [Fact]
        public async Task Create_NestedObjectDifferentLevelProjection()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { foo { barString nestedObject { foo {barShort}}}}}")
                    .Create());

            res1.MatchSqlSnapshot("12");
        }

        [Fact]
        public async Task Create_ListObjectTwoProjections()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { foo { objectArray { foo { barString barShort}}}}}")
                    .Create());

            res1.MatchSqlSnapshot("12");
        }

        [Fact]
        public async Task Create_ListObjectDifferentLevelProjection()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities, OnModelCreating);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { foo { barString objectArray{ foo {barShort}}}}}")
                    .Create());

            res1.MatchSqlSnapshot("12");
        }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Foo>().HasMany(x => x.ObjectArray);
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

            public List<BarDeep> ObjectArray { get; set; }

            public BarDeep NestedObject { get; set; }
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

            public List<BarNullable>? ObjectArray { get; set; }
        }

        public class Bar
        {
            public int Id { get; set; }

            public Foo Foo { get; set; }
        }

        public class BarDeep
        {
            public int Id { get; set; }

            public FooDeep Foo { get; set; }
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
            QUX
        }
    }
}
