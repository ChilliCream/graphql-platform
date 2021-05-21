using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JBooleanFilterTests
        : IClassFixture<Neo4JFixture>
    {
        private readonly Neo4JFixture _fixture;

        public Neo4JBooleanFilterTests(Neo4JFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly string _fooEntitiesCypher =
            @"CREATE (:Foo {Bar: true}), (:Foo {Bar: false})";
        private readonly string _fooEntitiesNullableCypher =
            @"CREATE
                (:FooNullable {Bar: true}),
                (:FooNullable {Bar: false}),
                (:FooNullable {Bar: NULL})";

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

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            const string query1 = "{ root(where: { bar: { eq: true}}){ bar }}";
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query1)
                    .Create());

            const string query2 = "{ root(where: { bar: { eq: false}}){ bar }}";
            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query2)
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("true");
            res2.MatchDocumentSnapshot("false");
        }

        [Fact]
        public async Task Create_And_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            const string query1 =
                "{ root(where: {and: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar }}";
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query1)
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("and");
        }

        [Fact]
        public async Task Create_Or_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            const string query1 =
                "{ root(where: {or: [{ bar: { eq: true}}, { bar: { eq: false}}]} ){ bar }}";
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query1)
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("or");
        }

        [Fact]
        public async Task Create_BooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            const string query1 = "{ root(where: { bar: { neq: true}}){ bar}}";
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query1)
                    .Create());

            const string query2 = "{ root(where: { bar: { neq: false}}){ bar}}";
            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query2)
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("true");
            res2.MatchDocumentSnapshot("false");
        }

        [Fact]
        public async Task Create_NullableBooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(
                    _fooEntitiesNullableCypher);

            // act
            const string query1 = "{ root(where: { bar: { eq: true}}){ bar }}";
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query1)
                    .Create());

            const string query2 = "{ root(where: { bar: { eq: false}}){ bar }}";
            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query2)
                    .Create());

            const string query3 = "{ root(where: { bar: { eq: null}}){ bar }}";
            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query3)
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("true");
            res2.MatchDocumentSnapshot("false");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(
                    _fooEntitiesNullableCypher);

            // act
            const string query1 = "{ root(where: { bar: { neq: true}}){ bar }}";
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query1)
                    .Create());

            const string query2 = "{ root(where: { bar: { neq: false}}){ bar }}";
            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query2)
                    .Create());

            const string query3 = "{ root(where: { bar: { neq: null}}){ bar }}";
            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query3)
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("true");
            res2.MatchDocumentSnapshot("false");
            res3.MatchDocumentSnapshot("null");
        }
    }
}
