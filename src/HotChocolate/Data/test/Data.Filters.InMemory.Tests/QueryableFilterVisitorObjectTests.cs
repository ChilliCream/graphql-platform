using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterVisitorObjectTests
        : IClassFixture<SchemaCache>
    {
       private static readonly Bar[] _barEntities = new[]{
            new Bar {
                Foo = new Foo {
                    BarShort = 12,
                    BarBool = true,
                    BarEnum = BarEnum.BAR,
                    BarString = "testatest",
                    //ScalarArray = new[] { "c", "d", "a" },
                    ObjectArray = new List<Bar> {
                        new Bar {
                            Foo = new Foo {
                                // ScalarArray = new[] { "c", "d", "a" }
                                BarShort = 12,
                                BarString = "a"
                            }
                        }
                    }
                }
             },
            new Bar {
                Foo = new Foo {
                    BarShort = 14,
                    BarBool = true,
                    BarEnum = BarEnum.BAZ,
                    BarString = "testbtest",
                    //ScalarArray = new[] { "c", "d", "b" },
                    ObjectArray = new List<Bar> {
                        new Bar {
                            Foo = new Foo {
                                //ScalarArray = new[] { "c", "d", "b" }
                                BarShort = 14,
                                BarString = "d"
                            }
                        }
                    }
                }
            },
            new Bar {
                Foo = new Foo {
                    BarShort = 13,
                    BarBool = false,
                    BarEnum = BarEnum.FOO,
                    BarString = "testctest",
                    //ScalarArray = null,
                    ObjectArray = null,}
            }};

        private static readonly BarNullable[] _barNullableEntities = new[]{
            new BarNullable {
                Foo = new FooNullable {
                    BarShort = 12,
                    BarBool = true,
                    BarEnum = BarEnum.BAR,
                    BarString = "testatest",
                    //ScalarArray = new[] { "c", "d", "a" },
                    ObjectArray = new List<BarNullable> {
                        new BarNullable {
                            Foo = new FooNullable{
                                //ScalarArray = new[] { "c", "d", "a" }
                                BarShort = 12,
                            }
                        }
                    }
                }
            },
            new BarNullable {
                Foo = new FooNullable {
                    BarShort = null,
                    BarBool = null,
                    BarEnum = BarEnum.BAZ,
                    BarString = "testbtest",
                    //ScalarArray = new[] { "c", "d", "b" },
                    ObjectArray = new List<BarNullable> {
                        new BarNullable {
                            Foo = new FooNullable{
                                //ScalarArray = new[] { "c", "d", "b" }
                                BarShort = null,
                            }
                        }
                    }
                }
            },
            new BarNullable {
                Foo = new FooNullable {
                    BarShort = 14,
                    BarBool = false,
                    BarEnum = BarEnum.QUX,
                    BarString = "testctest",
                    //ScalarArray = null,
                    ObjectArray = new List<BarNullable> {
                        new BarNullable {
                            Foo = new FooNullable{
                                //ScalarArray = new[] { "c", "d", "b" }
                                BarShort = 14,
                            }
                        }
                    }
                }
            },
            new BarNullable {
                Foo = new FooNullable {
                    BarShort = 13,
                    BarBool = false,
                    BarEnum = BarEnum.FOO,
                    BarString = "testdtest",
                    //ScalarArray = null,
                    ObjectArray = null
                }
            }};

        private readonly SchemaCache _cache;

        public QueryableFilterVisitorObjectTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_ObjectShortEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: 12}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: 13}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: null}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectShortIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 12, 13 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res1.MatchSnapshot("12and13");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ null, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res2.MatchSnapshot("13and14");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ null, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res3.MatchSnapshot("nullAnd14");
        }

        [Fact]
        public async Task Create_ObjectNullableShortEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester =
                _cache.CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: 12}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: 13}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { eq: null}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectNullableShortIn_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<BarNullable, BarNullableFilterType>(_barNullableEntities);

            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 12, 13 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res1.MatchSnapshot("12and13");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 13, 14 ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res2.MatchSnapshot("13and14");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barShort: { in: [ 13, null ]}}}) " +
                    "{ foo{ barShort}}}")
                .Create());

            res3.MatchSnapshot("13andNull");
        }

        [Fact]
        public async Task Create_ObjectBooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: true}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

            res1.MatchSnapshot("true");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: false}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

            res2.MatchSnapshot("false");
        }

        [Fact]
        public async Task Create_ObjectNullableBooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<BarNullable, BarNullableFilterType>(
                _barNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: true}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

            res1.MatchSnapshot("true");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: false}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

            res2.MatchSnapshot("false");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barBool: { eq: null}}}) " +
                    "{ foo{ barBool}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectEnumEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: BAR}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res1.MatchSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: FOO}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: null}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectEnumIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ BAR FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res1.MatchSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ null FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res3.MatchSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_ObjectNullableEnumEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<BarNullable, BarNullableFilterType>(
                _barNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: BAR}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res1.MatchSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: FOO}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { eq: null}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectNullableEnumIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<BarNullable, BarNullableFilterType>(
                _barNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ BAR FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res1.MatchSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res2.MatchSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barEnum: { in: [ null FOO ]}}}) " +
                    "{ foo{ barEnum}}}")
                .Create());

            res3.MatchSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_ObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { eq: \"testatest\"}}}) " +
                    "{ foo{ barString}}}")
                .Create());

            res1.MatchSnapshot("testatest");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { eq: \"testbtest\"}}}) " +
                    "{ foo{ barString}}}")
                .Create());

            res2.MatchSnapshot("testbtest");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { eq: null}}}){ foo{ barString}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectStringIn_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                   "{ root(where: { foo: { barString: { in: [ \"testatest\"  \"testbtest\" ]}}}) " +
                   "{ foo{ barString}}}")
                .Create());

            res1.MatchSnapshot("testatestAndtestb");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { in: [\"testbtest\" null]}}}) " +
                    "{ foo{ barString}}}")
                .Create());

            res2.MatchSnapshot("testbtestAndNull");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { barString: { in: [ \"testatest\" ]}}}) " +
                    "{ foo{ barString}}}")
                .Create());

            res3.MatchSnapshot("testatest");
        }

        [Fact]
        public async Task Create_ArrayObjectNestedArraySomeStringEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo:{ objectArray: { " +
                             "some: { foo: { barString: { eq: \"a\"}}}}}}) " +
                    "{ foo { objectArray { foo { barString}}}}}")
                .Create());

            res1.MatchSnapshot("a");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo:{ objectArray: { " +
                             "some: { foo: { barString: { eq: \"d\"}}}}}}) " +
                    "{ foo { objectArray { foo { barString}}}}}")
                .Create());

            res2.MatchSnapshot("d");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo:{ objectArray: { " +
                             "some: { foo: { barString: { eq: null}}}}}}) " +
                    "{ foo { objectArray { foo {barString}}}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayObjectNestedArrayAnyStringEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Bar, BarFilterType>(_barEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { objectArray: { any: false}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Create());

            res1.MatchSnapshot("false");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { objectArray: { any: true}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Create());

            res2.MatchSnapshot("true");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { foo: { objectArray: { any: null}}}) " +
                    "{ foo { objectArray  { foo { barString }}}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        public class Foo
        {
            public int Id { get; set; }

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
            public int Id { get; set; }

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
            public int Id { get; set; }

            public Foo Foo { get; set; } = null!;

        }

        public class BarNullable
        {
            public int Id { get; set; }

            public FooNullable? Foo { get; set; }

        }

        public class BarFilterType
            : FilterInputType<Bar>
        {
        }

        public class BarNullableFilterType
            : FilterInputType<BarNullable>
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
