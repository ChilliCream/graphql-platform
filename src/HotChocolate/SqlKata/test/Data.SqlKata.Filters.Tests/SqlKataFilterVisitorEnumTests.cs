using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.SqlKata.Filters
{
    public class SqlKataFilterVisitorEnumTests
    {
        private static readonly Foo[] _fooEntities =
        {
            new() { BarEnum = FooEnum.BAR },
            new() { BarEnum = FooEnum.BAZ },
            new() { BarEnum = FooEnum.FOO },
            new() { BarEnum = FooEnum.QUX }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new() { BarEnum = FooEnum.BAR },
            new() { BarEnum = FooEnum.BAZ },
            new() { BarEnum = FooEnum.FOO },
            new() { BarEnum = null },
            new() { BarEnum = FooEnum.QUX }
        };

        private readonly SchemaCache _cache = new();

        [Fact]
        public async Task Create_EnumEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: BAR } }) { barEnum } }")
                    .Create());

            res1.MatchSqlSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: FOO } }) { barEnum } }")
                    .Create());

            res2.MatchSqlSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: null } }) { barEnum } }")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        [Fact]
        public async Task Create_EnumNotEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: BAR } }) { barEnum } }")
                    .Create());

            res1.MatchSqlSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: FOO } }) { barEnum } }")
                    .Create());

            res2.MatchSqlSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: null } }){ barEnum } }")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        [Fact]
        public async Task Create_EnumIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ BAR FOO ]}}){ barEnum}}")
                    .Create());

            res1.MatchSqlSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ FOO ]}}){ barEnum}}")
                    .Create());

            res2.MatchSqlSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ null FOO ]}}){ barEnum}}")
                    .Create());

            res3.MatchSqlSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_EnumNotIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ BAR FOO ] } }) { barEnum } }")
                    .Create());

            res1.MatchSqlSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ FOO ] } }) { barEnum } }")
                    .Create());

            res2.MatchSqlSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ null FOO ] } }) { barEnum } }")
                    .Create());

            res3.MatchSqlSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_NullableEnumEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: BAR } }) { barEnum } }")
                    .Create());

            res1.MatchSqlSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: FOO } }) { barEnum } }")
                    .Create());

            res2.MatchSqlSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: null } }){ barEnum } }")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableEnumNotEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: BAR } }) { barEnum } }")
                    .Create());

            res1.MatchSqlSnapshot("BAR");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: FOO } }) { barEnum } }")
                    .Create());

            res2.MatchSqlSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: null } }) { barEnum } }")
                    .Create());

            res3.MatchSqlSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableEnumIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ BAR FOO ] } }) { barEnum } }")
                    .Create());

            res1.MatchSqlSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ FOO ] } }) { barEnum } }")
                    .Create());

            res2.MatchSqlSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ null FOO ] } }) { barEnum } }")
                    .Create());

            res3.MatchSqlSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_NullableEnumNotIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ BAR FOO ] } }){ barEnum } }")
                    .Create());

            res1.MatchSqlSnapshot("BarAndFoo");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ FOO ] } }) { barEnum } }")
                    .Create());

            res2.MatchSqlSnapshot("FOO");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ null FOO ] } }) { barEnum } }")
                    .Create());

            res3.MatchSqlSnapshot("nullAndFoo");
        }

        [Table("ExampleData")]
        public class Foo
        {
            public int Id { get; set; }

            public FooEnum BarEnum { get; set; }
        }

        [Table("ExampleData")]
        public class FooNullable
        {
            public int Id { get; set; }

            public FooEnum? BarEnum { get; set; }
        }

        public enum FooEnum
        {
            FOO,
            BAR,
            BAZ,
            QUX
        }

        public class FooFilterInput
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterInput
            : FilterInputType<FooNullable>
        {
        }
    }
}
