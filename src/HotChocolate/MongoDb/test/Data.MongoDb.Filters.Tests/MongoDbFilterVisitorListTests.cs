using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class MongoDbFilterVisitorListTests
        : SchemaCache
        , IClassFixture<MongoResource>
    {
        private static readonly Foo[] _fooEntities =
        {
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
            },
            new Foo { FooNested = null },
            new Foo { FooNested = new FooNested[0] }
        };

        private static readonly FooSimple[] _fooSimple = new[]
        {
            new FooSimple
            {
                Bar = new[]
                {
                    "a",
                    "a",
                    "a"
                }
            },
            new FooSimple
            {
                Bar = new[]
                {
                    "c",
                    "a",
                    "a"
                }
            },
            new FooSimple
            {
                Bar = new[]
                {
                    "a",
                    "d",
                    "b"
                }
            },
            new FooSimple
            {
                Bar = new[]
                {
                    "c",
                    "d",
                    "b"
                }
            },
            new FooSimple
            {
                Bar = new[]
                {
                    null,
                    "d",
                    "b"
                }
            },
            new FooSimple { Bar = null },
            new FooSimple { Bar = new string[0] }
        };

        public MongoDbFilterVisitorListTests(MongoResource resource)
        {
            Init(resource);
        }

        [Fact]
        public async Task Create_ArraySomeObjectStringEqualWithNull_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            root(where: {
                                fooNested: {
                                    some: {
                                        bar: {
                                            eq: ""a""
                                        }
                                    }
                                }
                            }){
                                fooNested {
                                    bar
                                }
                            }
                        }")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { some: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                    .Create());

            res2.MatchDocumentSnapshot("d");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { some: {bar: { eq: null}}}}){ fooNested {bar}}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayNoneObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { none: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { none: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                    .Create());

            res2.MatchDocumentSnapshot("d");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { none: {bar: { eq: null}}}}){ fooNested {bar}}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayAllObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { all: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { all: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                    .Create());

            res2.MatchDocumentSnapshot("d");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { all: {bar: { eq: null}}}}){ fooNested {bar}}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayAnyObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { fooNested: { any: false}}){ fooNested {bar}}}")
                    .Create());

            res1.MatchDocumentSnapshot("false");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { fooNested: { any: true}}){ fooNested {bar}}}")
                    .Create());

            res2.MatchDocumentSnapshot("true");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { fooNested: { all: null}}){ fooNested {bar}}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ArraySomeStringEqualWithNull_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooSimple, FooSimpleFilterType>(_fooSimple);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"{
                            root(where: {
                                bar: {
                                    some: {
                                        eq: ""a""
                                    }
                                }
                            }){
                                bar
                            }
                        }")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { some: { eq: \"d\"}}}){ bar }}")
                    .Create());

            res2.MatchDocumentSnapshot("d");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { some: { eq: null}}}){ bar }}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayNoneStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooSimple, FooSimpleFilterType>(_fooSimple);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { none: { eq: \"a\"}}}){ bar }}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { none: { eq: \"d\"}}}){ bar }}")
                    .Create());

            res2.MatchDocumentSnapshot("d");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { none: { eq: null}}}){ bar }}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayAllStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooSimple, FooSimpleFilterType>(_fooSimple);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { all: { eq: \"a\"}}}){ bar }}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { all: { eq: \"d\"}}}){ bar }}")
                    .Create());

            res2.MatchDocumentSnapshot("d");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { bar: { all: { eq: null}}}){ bar }}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ArrayAnyStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooSimple, FooSimpleFilterType>(_fooSimple);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { any: false}}){ bar }}")
                    .Create());

            res1.MatchDocumentSnapshot("false");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { any: true}}){ bar }}")
                    .Create());

            res2.MatchDocumentSnapshot("true");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { all: null}}){ bar }}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public IEnumerable<FooNested?>? FooNested { get; set; }
        }

        public class FooSimple
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public IEnumerable<string?>? Bar { get; set; }
        }

        public class FooNested
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public string? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.FooNested);
            }
        }

        public class FooSimpleFilterType
            : FilterInputType<FooSimple>
        {
            protected override void Configure(IFilterInputTypeDescriptor<FooSimple> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }
    }
}
