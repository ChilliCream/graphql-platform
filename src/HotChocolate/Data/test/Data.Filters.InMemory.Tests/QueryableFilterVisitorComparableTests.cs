using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class QueryableFilterVisitorComparableTests
        : IClassFixture<SchemaCache>
    {
        private static readonly Foo[] _fooEntities = new[]
        {
            new Foo { BarShort = 12 },
            new Foo { BarShort = 14 },
            new Foo { BarShort = 13 }
        };

        private static readonly FooNullable[] _fooNullableEntities = new[]
        {
            new FooNullable { BarShort = 12 },
            new FooNullable { BarShort = null },
            new FooNullable { BarShort = 14 },
            new FooNullable { BarShort = 13 }
        };

        private readonly SchemaCache _cache;

        public QueryableFilterVisitorComparableTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_ShortEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: null}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNotEqual_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: null}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortGreaterThan_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gt: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gt: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gt: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gt: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNotGreaterThan_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngt: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngt: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngt: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngt: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }


        [Fact]
        public async Task Create_ShortGreaterThanOrEquals_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gte: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gte: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gte: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gte: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNotGreaterThanOrEquals_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngte: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngte: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngte: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngte: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortLowerThan_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lt: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lt: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lt: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lt: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNotLowerThan_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlt: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlt: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlt: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlt: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }


        [Fact]
        public async Task Create_ShortLowerThanOrEquals_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lte: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lte: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lte: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lte: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNotLowerThanOrEquals_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlte: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlte: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlte: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlte: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12and13");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { in: [ null, 14 ]}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13and14");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { in: [ null, 14 ]}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("nullAnd14");
        }

        [Fact]
        public async Task Create_ShortNotIn_Expression()
        {
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12and13");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nin: [ null, 14 ]}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13and14");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nin: [ null, 14 ]}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("nullAnd14");
        }

        [Fact]
        public async Task Create_ShortNullableEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { eq: null}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNullableNotEqual_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { neq: null}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("null");
        }


        [Fact]
        public async Task Create_ShortNullableGreaterThan_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gt: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gt: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gt: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gt: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNullableNotGreaterThan_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngt: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngt: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngt: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngt: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }


        [Fact]
        public async Task Create_ShortNullableGreaterThanOrEquals_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gte: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gte: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gte: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { gte: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNullableNotGreaterThanOrEquals_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngte: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngte: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngte: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { ngte: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNullableLowerThan_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lt: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lt: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lt: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lt: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNullableNotLowerThan_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlt: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlt: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlt: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlt: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }


        [Fact]
        public async Task Create_ShortNullableLowerThanOrEquals_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lte: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lte: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lte: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { lte: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNullableNotLowerThanOrEquals_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlte: 12}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlte: 13}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlte: 14}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("14");

            IExecutionResult? res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nlte: null}}){ barShort}}")
                .Create());

            res4.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ShortNullableIn_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { in: [ 12, 13 ]}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12and13");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { in: [ 13, 14 ]}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13and14");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { in: [ 13, null ]}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("13andNull");
        }

        [Fact]
        public async Task Create_ShortNullableNotIn_Expression()
        {
            IRequestExecutor? tester =
                _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nin: [ 12, 13 ]}}){ barShort}}")
                .Create());

            res1.MatchSnapshot("12and13");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nin: [ 13, 14 ]}}){ barShort}}")
                .Create());

            res2.MatchSnapshot("13and14");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { barShort: { nin: [ 13, null ]}}){ barShort}}")
                .Create());

            res3.MatchSnapshot("13andNull");
        }

        public class Foo
        {
            public int Id { get; set; }

            public short BarShort { get; set; }

            public int BarInt { get; set; }

            public long BarLong { get; set; }

            public float BarFloat { get; set; }

            public double BarDouble { get; set; }

            public decimal BarDecimal { get; set; }
        }

        public class FooNullable
        {
            public int Id { get; set; }
            public short? BarShort { get; set; }
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
