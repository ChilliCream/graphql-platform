using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Xunit;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Squadron;
using System;

namespace HotChocolate.Types.Sorting
{
    [Obsolete]
    public class MongoSortingTests : IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public MongoSortingTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }


        [Fact]
        public async Task GetItems_NoSorting_AllItems_Are_Returned_Unsorted()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton(_ =>
                {
                    var database = _mongoResource.CreateDatabase();
                    var collection = database.GetCollection<Model>("col");
                    collection.InsertMany(new[]
                    {
                        new Model { Foo = "abc", Bar = 1, Baz = true },
                        new Model { Foo = "def", Bar = 2, Baz = false },
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();

            var executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            var request = QueryRequestBuilder.New()
                .SetQuery("{ items { foo } }")
                .Create();

            // act
            var result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton(_ =>
                {
                    var database = _mongoResource.CreateDatabase();
                    var collection = database.GetCollection<Model>("col");
                    collection.InsertMany(new[]
                    {
                        new Model { Foo = "abc", Bar = 1, Baz = true },
                        new Model { Foo = "def", Bar = 2, Baz = false },
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();

            var executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            var request = QueryRequestBuilder.New()
                .SetQuery("{ items(order_by: { foo: DESC }) { foo } }")
                .Create();

            // act
            var result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_With_Paging__DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton(_ =>
                {
                    var database = _mongoResource.CreateDatabase();
                    var collection = database.GetCollection<Model>("col");
                    collection.InsertMany(new[]
                    {
                        new Model { Foo = "abc", Bar = 1, Baz = true },
                        new Model { Foo = "def", Bar = 2, Baz = false },
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();

            var executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            var request = QueryRequestBuilder.New()
                .SetQuery("{ paging(order_by: { foo: DESC }) { nodes { foo } } }")
                .Create();

            // act
            var result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.ToJson().MatchSnapshot();
        }


        [Fact]
        public async Task GetItems_OnRenamedField_DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton(_ =>
                {
                    var database = _mongoResource.CreateDatabase();
                    var collection = database.GetCollection<Model>("col");
                    collection.InsertMany(new[]
                    {
                        new Model { Foo = "abc", Bar = 1, Baz = true },
                        new Model { Foo = "def", Bar = 2, Baz = false },
                    });
                    return collection;
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider();

            var executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            var request = QueryRequestBuilder.New()
                .SetQuery("{ items(order_by: { qux: DESC }) { bar } }")
                .Create();

            // act
            var result = await executor.ExecuteAsync(request);

            // assert
            Assert.Null(result.ExpectQueryResult().Errors);
            result.ToJson().MatchSnapshot();
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("items")
                    .Type<ListType<ModelType>>()
                    .UseSorting<ModelSortInputType>()
                    .Resolve(ctx =>
                        ctx.Service<IMongoCollection<Model>>().AsQueryable());

                descriptor.Field("paging")
                    .UsePaging<ModelType>()
                    .UseSorting<ModelSortInputType>()
                    .Resolve(ctx =>
                        ctx.Service<IMongoCollection<Model>>().AsQueryable());
            }
        }

        public class ModelSortInputType : SortInputType<Model>
        {
            protected override void Configure(ISortInputTypeDescriptor<Model> descriptor)
            {
                descriptor.Sortable(m => m.Bar).Name("qux");
            }
        }

        public class ModelType : ObjectType<Model>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Model> descriptor)
            {
                descriptor.Field(t => t.Id)
                    .Type<IdType>()
                    .Resolve(c => c.Parent<Model>().Id);
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
