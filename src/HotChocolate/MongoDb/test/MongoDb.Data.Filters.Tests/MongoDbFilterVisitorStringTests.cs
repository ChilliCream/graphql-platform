using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class QueryableFilterVisitorStringTests
        : SchemaCache,
          IClassFixture<MongoResource>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = "testatest" }, new Foo { Bar = "testbtest" }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = "testatest" },
            new FooNullable { Bar = "testbtest" },
            new FooNullable { Bar = null }
        };

        public QueryableFilterVisitorStringTests(MongoResource resource)
        {
            Init(resource);
        }

        [Fact]
        public async Task Create_StringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"testatest\"}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testatest");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"testbtest\"}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testbtest");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"testatest\"}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testatest");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"testbtest\"}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testbtest");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringIn_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testatestAndtestb");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testbtestAndNull");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("testatest");
        }

        [Fact]
        public async Task Create_StringNotIn_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testatestAndtestb");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testbtestAndNull");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("testatest");
        }

        [Fact]
        public async Task Create_StringContains_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: \"a\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: \"b\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNoContains_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: \"a\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: \"b\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringStartsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: \"testa\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testa");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: \"testb\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testb");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotStartsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testa");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testb");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringEndsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: \"atest\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("atest");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: \"btest\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("btest");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_StringNotEndsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: \"atest\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("atest");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: \"btest\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("btest");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"testatest\"}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testatest");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"testbtest\"}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testbtest");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"testatest\"}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testatest");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"testbtest\"}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testbtest");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringIn_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testatestAndtestb");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testbtestAndNull");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("testatest");
        }

        [Fact]
        public async Task Create_NullableStringNotIn_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testatestAndtestb");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testbtestAndNull");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("testatest");
        }

        [Fact]
        public async Task Create_NullableStringContains_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: \"a\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: \"b\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { contains: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNoContains_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: \"a\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: \"b\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { ncontains: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringStartsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: \"testa\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testa");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: \"testb\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testb");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { startsWith: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotStartsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("testa");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("testb");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nstartsWith: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringEndsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: \"atest\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("atest");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: \"btest\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("btest");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { endsWith: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableStringNotEndsWith_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: \"atest\" }}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("atest");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: \"btest\" }}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("btest");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { nendsWith: null }}){ bar}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public string Bar { get; set; } = null!;
        }

        public class FooNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public string? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }
    }
}
