using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JStringFilterTests
        : IClassFixture<Neo4JFixture>
    {
        private readonly Neo4JFixture _fixture;

        public Neo4JStringFilterTests(Neo4JFixture fixture)
        {
            _fixture = fixture;
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooFilterType : FilterInputType<Foo> { }

        private string _fooEntitiesCypher = @"CREATE (:Foo {Bar: 'testatest'}), (:Foo {Bar: 'testbtest'})";

        private string _fooNullableEntitiesCypher = @"CREATE (:FooNullable {Bar: 'testatest'}), (:FooNullable {Bar: 'testbtest'}), (:FooNullable {Bar: NULL})";

        public class FooNullable
        {
            public string? Bar { get; set; }
        }

        public class FooNullableFilterType : FilterInputType<FooNullable> { }

        [Fact]
        public async Task Create_StringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"testatest\"}}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"testbtest\"}}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null}}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testatest");
            res2.MatchDocumentSnapshot("testbtest");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"testatest\"}}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"testbtest\"}}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: null}}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testatest");
            res2.MatchDocumentSnapshot("testbtest");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringStartsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: \"testa\" }}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: \"testb\" }}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: null }}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testa");
            res2.MatchDocumentSnapshot("testb");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotStartsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: null }}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testa");
            res2.MatchDocumentSnapshot("testb");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringIn_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testatestAndtestb");
            res2.MatchDocumentSnapshot("testbtestAndNull");
            res3.MatchDocumentSnapshot("testatest");
        }

        [Fact]
        public async Task Create_StringNotIn_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testatestAndtestb");
            res2.MatchDocumentSnapshot("testbtestAndNull");
            res3.MatchDocumentSnapshot("testatest");
        }

        [Fact]
        public async Task Create_StringContains_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: \"a\" }}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: \"b\" }}){ bar}}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: null }}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("a");
            res2.MatchDocumentSnapshot("b");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotContains_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: \"a\" }}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: \"b\" }}){ bar}}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: null }}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("a");
            res2.MatchDocumentSnapshot("b");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringEndsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: \"atest\" }}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: \"btest\" }}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: null }}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("atest");
            res2.MatchDocumentSnapshot("btest");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotEndsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: \"atest\" }}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: \"btest\" }}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: null }}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("atest");
            res2.MatchDocumentSnapshot("btest");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"testatest\"}}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"testbtest\"}}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testatest");
            res2.MatchDocumentSnapshot("testbtest");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"testatest\"}}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"testbtest\"}}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testatest");
            res2.MatchDocumentSnapshot("testbtest");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringIn_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testatestAndtestb");
            res2.MatchDocumentSnapshot("testbtestAndNull");
            res3.MatchDocumentSnapshot("testatest");
        }

        [Fact]
        public async Task Create_NullableStringNotIn_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testatestAndtestb");
            res2.MatchDocumentSnapshot("testbtestAndNull");
            res3.MatchDocumentSnapshot("testatest");
        }

        [Fact]
        public async Task Create_NullableStringContains_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: \"a\" }}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: \"b\" }}){ bar}}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: null }}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("a");
            res2.MatchDocumentSnapshot("b");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotContains_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: \"a\" }}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: \"b\" }}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: null }}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("a");
            res2.MatchDocumentSnapshot("b");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringStartsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: \"testa\" }}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: \"testb\" }}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: null }}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testa");
            res2.MatchDocumentSnapshot("testb");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotStartsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: null }}){ bar}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("testa");
            res2.MatchDocumentSnapshot("testb");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringEndsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: \"atest\" }}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: \"btest\" }}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: null }}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("atest");
            res2.MatchDocumentSnapshot("btest");
            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotEndsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntitiesCypher);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: \"atest\" }}){ bar }}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: \"btest\" }}){ bar }}")
                    .Create());

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: null }}){ bar }}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("atest");
            res2.MatchDocumentSnapshot("btest");
            res3.MatchDocumentSnapshot("null");
        }
    }
}
