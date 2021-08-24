using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class MongoDbFilterVisitorObjectIdTests
        : SchemaCache
        , IClassFixture<MongoResource>
    {
        private static readonly Foo[] _fooEntities =
        {
            new() { ObjectId = new ObjectId("a") },
            new() { ObjectId = new ObjectId("b") },
            new() { ObjectId = new ObjectId("c") }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new() { ObjectId = new ObjectId("a") },
            new() { },
            new() { ObjectId = new ObjectId("b") },
            new() { ObjectId = new ObjectId("c") }
        };

        public MongoDbFilterVisitorObjectIdTests(MongoResource resource)
        {
            Init(resource);
        }

        [Fact]
        public async Task Create_ObjectIdEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { eq: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { eq: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { eq: null}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNotEqual_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { neq: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { neq: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { neq: null}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdGreaterThan_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNotGreaterThan_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }


        [Fact]
        public async Task Create_ObjectIdGreaterThanOrEquals_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNotGreaterThanOrEquals_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdLowerThan_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNotLowerThan_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }


        [Fact]
        public async Task Create_ObjectIdLowerThanOrEquals_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNotLowerThanOrEquals_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdIn_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ \"a\", \"b\" ]}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("aandb");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ null, \"c\" ]}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("bandc");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ null, \"c\" ]}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("nullAndc");
        }

        [Fact]
        public async Task Create_ObjectIdNotIn_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ \"a\", \"b\" ]}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("aandb");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ null, \"c\" ]}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("bandc");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ null, \"c\" ]}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("nullAndc");
        }

        [Fact]
        public async Task Create_ObjectIdNullableEqual_Expression()
        {
            // arrange
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { eq: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { eq: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { eq: null}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNullableNotEqual_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { neq: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { neq: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { neq: null}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }


        [Fact]
        public async Task Create_ObjectIdNullableGreaterThan_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNullableNotGreaterThan_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }


        [Fact]
        public async Task Create_ObjectIdNullableGreaterThanOrEquals_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNullableNotGreaterThanOrEquals_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNullableLowerThan_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNullableNotLowerThan_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }


        [Fact]
        public async Task Create_ObjectIdNullableLowerThanOrEquals_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNullableNotLowerThanOrEquals_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"a\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"b\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"c\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("c");

            IExecutionResult res4 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: null}}){ objectId}}")
                    .Create());

            res4.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_ObjectIdNullableIn_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ \"a\", \"b\" ]}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("aandb");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ \"b\", \"c\" ]}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("bandc");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ \"b\", null ]}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("bandNull");
        }

        [Fact]
        public async Task Create_ObjectIdNullableNotIn_Expression()
        {
            IRequestExecutor tester =
                CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ \"a\", \"b\" ]}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("aandb");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ \"b\", \"c\" ]}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("bandc");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ \"b\", null ]}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("bandNull");
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public ObjectId ObjectId { get; set; }
        }

        public class FooNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public ObjectId? ObjectId { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }
    }
}
