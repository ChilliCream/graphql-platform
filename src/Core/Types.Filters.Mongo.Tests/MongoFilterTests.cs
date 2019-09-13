using System.Linq;
using System;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Xunit;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types.Filters
{
    public class MongoFilterTests
    {
        [Fact]
        public async Task GetItems_NoFilter_AllItems_Are_Returned()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IMongoCollection<Model>>(sp =>
            {
                MongoClient client = new MongoClient();
                IMongoDatabase database = client.GetDatabase(
                    "db_" + Guid.NewGuid().ToString("N"));

                var collection = database.GetCollection<Model>("col");
                collection.InsertMany(new[]
                {
                    new Model { Foo = "abc", Bar = 1, Baz = true },
                    new Model { Foo = "def", Bar = 2, Baz = false },
                });
                return collection;
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(serviceCollection.BuildServiceProvider())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ items { foo } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_EqualsFilter_FirstItems_Is_Returned()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IMongoCollection<Model>>(sp =>
            {
                MongoClient client = new MongoClient();
                IMongoDatabase database = client.GetDatabase(
                    "db_" + Guid.NewGuid().ToString("N"));

                var collection = database.GetCollection<Model>("col");
                collection.InsertMany(new[]
                {
                    new Model { Foo = "abc", Bar = 1, Baz = true },
                    new Model { Foo = "def", Bar = 2, Baz = false },
                });
                return collection;
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(serviceCollection.BuildServiceProvider())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ items(where: { foo: \"abc\" }) { foo } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_With_Paging_EqualsFilter_FirstItems_Is_Returned()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IMongoCollection<Model>>(sp =>
            {
                MongoClient client = new MongoClient();
                IMongoDatabase database = client.GetDatabase(
                    "db_" + Guid.NewGuid().ToString("N"));

                var collection = database.GetCollection<Model>("col");
                collection.InsertMany(new[]
                {
                    new Model { Foo = "abc", Bar = 1, Baz = true },
                    new Model { Foo = "def", Bar = 2, Baz = false },
                });
                return collection;
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(serviceCollection.BuildServiceProvider())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ paging(where: { foo: \"abc\" }) { nodes { foo } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("items")
                    .Type<ListType<ModelType>>()
                    .UseFiltering<FilterInputType<Model>>()
                    .Resolver(ctx =>
                        ctx.Service<IMongoCollection<Model>>().AsQueryable());

                descriptor.Field("paging")
                    .UsePaging<ModelType>()
                    .UseFiltering<FilterInputType<Model>>()
                    .Resolver(ctx =>
                        ctx.Service<IMongoCollection<Model>>().AsQueryable());
            }
        }

        public class ModelType : ObjectType<Model>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Model> descriptor)
            {
                descriptor.Field(t => t.Id)
                    .Type<IdType>()
                    .Resolver(c => c.Parent<Model>().Id);
            }
        }

        public class Model
        {
            public ObjectId Id { get; set; }
            public string Foo { get; set; }
            public int Bar { get; set; }
            public bool Baz { get; set; }
        }
    }
}
