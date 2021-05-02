using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JBooleanFilterTests
        : SchemaCache
        , IClassFixture<Neo4jResource<Neo4JConfig>>
    {
        public class Foo
        {
            public bool Bar { get; set; }
        }

        public class FooNullable
        {
            public bool? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }

        public Neo4JBooleanFilterTests(Neo4jResource<Neo4JConfig> neo4JResource)
        {
            Init(neo4JResource);
        }

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<Foo, FooFilterType>(
                @"CREATE (:Foo {Bar: true}), (:Foo {Bar: false})",
                false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("false");
        }

        [Fact]
        public async Task Create_And_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<Foo, FooFilterType>(
                @"CREATE (:Foo {Bar: true}), (:Foo {Bar: false})",
                false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: {and: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("and");
        }

        [Fact]
        public async Task Create_Or_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<Foo, FooFilterType>(
                @"CREATE (:Foo {Bar: true}), (:Foo {Bar: false})",
                false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: {or: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("or");
        }

        [Fact]
        public async Task Create_BooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<Foo, FooFilterType>(
                @"CREATE (:Foo {Bar: true}), (:Foo {Bar: false})",
                false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("false");
        }

        [Fact]
        public async Task Create_NullableBooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<FooNullable, FooNullableFilterType>(
                @"CREATE (:FooNullable {Bar: true}), (:FooNullable {Bar: false}), (:FooNullable {Bar: NULL})",
                false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("false");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<FooNullable, FooNullableFilterType>(
                @"CREATE (:FooNullable {Bar: true}), (:FooNullable {Bar: false}), (:FooNullable {Bar: null})",
                false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("false");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }
    }
}
