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
            new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f69") },
            new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f6a") },
            new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f6b") }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f69") },
            new() { },
            new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f6a") },
            new() { ObjectId = new ObjectId("6124e80f3f5fc839830c1f6b") }
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
                    .SetQuery("{ root(where: { objectId: { eq: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { eq: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

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
                    .SetQuery("{ root(where: { objectId: { neq: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { neq: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

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
                    .SetQuery("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { in: [ \"6124e80f3f5fc839830c1f69\", \"6124e80f3f5fc839830c1f6a\" ]}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69and6124e80f3f5fc839830c1f6a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ null, \"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("band6124e80f3f5fc839830c1f6b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ null, \"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("nullAnd6124e80f3f5fc839830c1f6b");
        }

        [Fact]
        public async Task Create_ObjectIdNotIn_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ \"6124e80f3f5fc839830c1f69\", \"6124e80f3f5fc839830c1f6a\" ]}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69and6124e80f3f5fc839830c1f6a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ null, \"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("band6124e80f3f5fc839830c1f6b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ null, \"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("nullAnd6124e80f3f5fc839830c1f6b");
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
                    .SetQuery("{ root(where: { objectId: { eq: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { eq: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

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
                    .SetQuery("{ root(where: { objectId: { neq: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { neq: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

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
                    .SetQuery("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { gte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { ngte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlt: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { lte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f69\"}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f6a\"}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6a");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nlte: \"6124e80f3f5fc839830c1f6b\"}}){ objectId}}")
                    .Create());

            res3.MatchDocumentSnapshot("6124e80f3f5fc839830c1f6b");

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
                    .SetQuery("{ root(where: { objectId: { in: [ \"6124e80f3f5fc839830c1f69\", \"6124e80f3f5fc839830c1f6a\" ]}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69and6124e80f3f5fc839830c1f6a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ \"6124e80f3f5fc839830c1f6a\", \"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("band6124e80f3f5fc839830c1f6b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { in: [ \"6124e80f3f5fc839830c1f6a\", null ]}}){ objectId}}")
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
                    .SetQuery("{ root(where: { objectId: { nin: [ \"6124e80f3f5fc839830c1f69\", \"6124e80f3f5fc839830c1f6a\" ]}}){ objectId}}")
                    .Create());

            res1.MatchDocumentSnapshot("6124e80f3f5fc839830c1f69and6124e80f3f5fc839830c1f6a");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ \"6124e80f3f5fc839830c1f6a\", \"6124e80f3f5fc839830c1f6b\" ]}}){ objectId}}")
                    .Create());

            res2.MatchDocumentSnapshot("band6124e80f3f5fc839830c1f6b");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { objectId: { nin: [ \"6124e80f3f5fc839830c1f6a\", null ]}}){ objectId}}")
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
