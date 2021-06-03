using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.Data.MongoDb.Sorting
{
    public class MongoDbSortVisitorObjectTests
        : SchemaCache,
          IClassFixture<MongoResource>
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
                    ObjectArray = new List<Bar>
                    {
                        new Bar { Foo = new Foo { BarShort = 12, BarString = "a" } }
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
                    ObjectArray = new List<Bar>
                    {
                        new Bar { Foo = new Foo { BarShort = 14, BarString = "d" } }
                    }
                }
            },
            new Bar
            {
                Foo = new Foo
                {
                    BarShort = 13,
                    BarBool = false,
                    BarEnum = BarEnum.FOO,
                    BarString = "testctest",
                    ObjectArray = null,
                }
            }
        };

        private static readonly BarNullable?[] _barNullableEntities =
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
                        new BarNullable { Foo = new FooNullable { BarShort = 12 } }
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
                        new BarNullable { Foo = new FooNullable { BarShort = null } }
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
            },
            new BarNullable { Foo = null }
        };

        public MongoDbSortVisitorObjectTests(MongoResource resource)
        {
            Init(resource);
        }

        [Fact]
        public async Task Create_ObjectShort_OrderBy()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Bar, BarSortType>(_barEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barShort: ASC}}) " +
                        "{ foo{ barShort}}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barShort: DESC}}) " +
                        "{ foo{ barShort}}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        [Fact]
        public async Task Create_ObjectNullableShort_OrderBy()
        {
            // arrange
            IRequestExecutor? tester =
                CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barShort: ASC}}) " +
                        "{ foo{ barShort}}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barShort: DESC}}) " +
                        "{ foo{ barShort}}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("13");
        }

        [Fact]
        public async Task Create_ObjectEnum_OrderBy()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Bar, BarSortType>(_barEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barEnum: ASC}}) " +
                        "{ foo{ barEnum}}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barEnum: DESC}}) " +
                        "{ foo{ barEnum}}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        [Fact]
        public async Task Create_ObjectNullableEnum_OrderBy()
        {
            // arrange
            IRequestExecutor tester =
                CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barEnum: ASC}}) " +
                        "{ foo{ barEnum}}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barEnum: DESC}}) " +
                        "{ foo{ barEnum}}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("13");
        }

        [Fact]
        public async Task Create_ObjectString_OrderBy()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Bar, BarSortType>(_barEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barString: ASC}}) " +
                        "{ foo{ barString}}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barString: DESC}}) " +
                        "{ foo{ barString}}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        [Fact]
        public async Task Create_ObjectNullableString_OrderBy()
        {
            // arrange
            IRequestExecutor tester =
                CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barString: ASC}}) " +
                        "{ foo{ barString}}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barString: DESC}}) " +
                        "{ foo{ barString}}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("13");
        }

        [Fact]
        public async Task Create_ObjectBool_OrderBy()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Bar, BarSortType>(_barEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barBool: ASC}}) " +
                        "{ foo{ barBool}}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barBool: DESC}}) " +
                        "{ foo{ barBool}}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        [Fact]
        public async Task Create_ObjectNullableBool_OrderBy()
        {
            // arrange
            IRequestExecutor tester =
                CreateSchema<BarNullable, BarNullableSortType>(_barNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barBool: ASC}}) " +
                        "{ foo{ barBool}}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(order: { foo: { barBool: DESC}}) " +
                        "{ foo{ barBool}}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("13");
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public short BarShort { get; set; }

            public string BarString { get; set; } = "";

            public BarEnum BarEnum { get; set; }

            public bool BarBool { get; set; }

            //Not supported in SQL
            //public string[] ScalarArray { get; set; }

            public List<Bar> ObjectArray { get; set; } = new List<Bar>();
        }

        public class FooNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public short? BarShort { get; set; }

            public string? BarString { get; set; }

            public BarEnum? BarEnum { get; set; }

            public bool? BarBool { get; set; }

            //Not supported in SQL
            //public string?[] ScalarArray { get; set; }

            public List<BarNullable>? ObjectArray { get; set; }
        }

        public class Bar
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public Foo Foo { get; set; } = null!;
        }

        public class BarNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public FooNullable? Foo { get; set; }
        }

        public class BarSortType
            : SortInputType<Bar>
        {
        }

        public class BarNullableSortType
            : SortInputType<BarNullable>
        {
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
