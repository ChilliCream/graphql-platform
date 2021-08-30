using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.MongoDb;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectIdTypeTests
    {
        [Fact]
        public async Task Should_MapObjectIdToScalar()
        {
            // arrange
            IRequestExecutor executor = await CreateSchema();

            // act
            string schema = executor.Schema.Print();

            // assert
            schema.MatchSnapshot();
        }

        [Fact]
        public async Task Should_ReturnObjectIdOnQuery()
        {
            // arrange
            IRequestExecutor executor = await CreateSchema();
            string query = @"
            {
                foo {
                    id
                }
            }
            ";

            // act
            IReadOnlyQueryRequest request = QueryRequestBuilder.Create(query);
            IExecutionResult result = await executor.ExecuteAsync(request, CancellationToken.None);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Should_ReturnInputOnQuery()
        {
            // arrange
            IRequestExecutor executor = await CreateSchema();
            string query = @"
            {
                loopback(objectId: ""6124e80f3f5fc839830c1f6b"")
            }";

            // act
            IReadOnlyQueryRequest request = QueryRequestBuilder.Create(query);
            IExecutionResult result = await executor.ExecuteAsync(request, CancellationToken.None);

            // assert
            result.ToJson().MatchSnapshot();
        }

        private ValueTask<IRequestExecutor> CreateSchema() =>
            new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddType<ObjectIdType>()
                .BuildRequestExecutorAsync();

        public class Query
        {
            public Foo GetFoo() => new() { Id = new ObjectId("6124e80f3f5fc839830c1f6b") };

            public ObjectId Loopback(ObjectId objectId) => objectId;
        }

        public class Foo
        {
            public ObjectId Id { get; set; }
        }
    }
}
