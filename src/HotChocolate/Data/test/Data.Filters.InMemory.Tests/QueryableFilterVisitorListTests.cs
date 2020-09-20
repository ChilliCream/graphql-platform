using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class QueryableFilterVisitorListTests
        : IClassFixture<SchemaCache>
    {
        private static readonly Foo[] _fooEntities = new[]{
            new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" }
                }
            },
            new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" }
                }
            },
            new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            },
            new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            },
            new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = null },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            }};

        private readonly SchemaCache _cache;

        public QueryableFilterVisitorListTests(
            SchemaCache cache)
        {
            _cache = cache;
        }

        [Fact]
        public async Task Create_ArraySomeObjectStringEqualWithNull_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { some: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                .Create());

            res1.MatchSnapshot("a");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { some: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Create());

            res2.MatchSnapshot("d");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { some: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayNoneObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { none: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                .Create());

            res1.MatchSnapshot("a");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { none: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Create());

            res2.MatchSnapshot("d");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { none: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayAllObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { all: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                .Create());

            res1.MatchSnapshot("a");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { all: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Create());

            res2.MatchSnapshot("d");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { all: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayAnyObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { any: false}}){ fooNested {bar}}}")
                .Create());

            res1.MatchSnapshot("false");

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { any: true}}){ fooNested {bar}}}")
                .Create());

            res2.MatchSnapshot("true");

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery(
                    "{ root(where: { fooNested: { all: null}}){ fooNested {bar}}}")
                .Create());

            res3.MatchSnapshot("null");
        }

        public class Foo
        {
            public int Id { get; set; }

            public IEnumerable<FooNested?>? FooNested { get; set; }
        }

        public class FooSimple
        {
            public IEnumerable<string?>? Bar { get; set; }
        }

        public class FooNested
        {
            public int Id { get; set; }

            public string? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.FooNested);
            }
        }

        public class FooSimpleFilterType
            : FilterInputType<FooSimple>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooSimple> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }
    }
}
