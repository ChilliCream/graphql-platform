using System;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Xunit;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types.Sorting
{
    public class MongoSortingObjectTests
    {
        [Fact]
        public async Task GetItems_NoSorting_AllItems_Are_Returned_Unsorted()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(sp =>
            {
                var client = new MongoClient();
                IMongoDatabase database = client.GetDatabase(
                    "db_" + Guid.NewGuid().ToString("N"));

                IMongoCollection<Parent> collection
                    = database.GetCollection<Parent>("col");
                collection.InsertMany(new[]
                {
                    new Parent {Model = new Model { Foo = "abc", Bar = 1, Baz = true } },
                    new Parent {Model = new Model { Foo = "def", Bar = 2, Baz = false }},
                });
                return collection;
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(serviceCollection.BuildServiceProvider())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ items { model {foo} } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(sp =>
            {
                var client = new MongoClient();
                IMongoDatabase database = client.GetDatabase(
                    "db_" + Guid.NewGuid().ToString("N"));

                IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                collection.InsertMany(new[]
                {
                    new Parent {Model = new Model { Foo = "abc", Bar = 1, Baz = true } },
                    new Parent {Model = new Model { Foo = "def", Bar = 2, Baz = false }},
                });
                return collection;
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(serviceCollection.BuildServiceProvider())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ items(order_by: { model: {foo: DESC }}) { model {foo }} }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task GetItems_With_Paging__DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(sp =>
            {
                var client = new MongoClient();
                IMongoDatabase database = client.GetDatabase(
                    "db_" + Guid.NewGuid().ToString("N"));

                IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                collection.InsertMany(new[]
                {
                    new Parent {Model = new Model { Foo = "abc", Bar = 1, Baz = true } },
                    new Parent {Model = new Model { Foo = "def", Bar = 2, Baz = false }},
                });
                return collection;
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(serviceCollection.BuildServiceProvider())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ paging(order_by: { model: {foo: DESC }} }) { nodes { model {foo } } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }


        [Fact]
        public async Task GetItems_OnRenamedField_DescSorting_AllItems_Are_Returned_DescSorted()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(sp =>
            {
                var client = new MongoClient();
                IMongoDatabase database = client.GetDatabase(
                    "db_" + Guid.NewGuid().ToString("N"));

                IMongoCollection<Parent> collection = database.GetCollection<Parent>("col");
                collection.InsertMany(new[]
                {
                    new Parent {Model = new Model { Foo = "abc", Bar = 1, Baz = true } },
                    new Parent {Model = new Model { Foo = "def", Bar = 2, Baz = false } },
                });
                return collection;
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(serviceCollection.BuildServiceProvider())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ items(order_by: { model:{ qux: DESC }}) { model{bar }} }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("items")
                    .Type<ListType<ParentType>>()
                    .UseSorting<ParentSortInputType>()
                    .Resolver(ctx =>
                        ctx.Service<IMongoCollection<Parent>>().AsQueryable());

                descriptor.Field("paging")
                    .UsePaging<ParentType>()
                    .UseSorting<ParentSortInputType>()
                    .Resolver(ctx =>
                        ctx.Service<IMongoCollection<Parent>>().AsQueryable());
            }
        }

        public class ParentSortInputType : SortInputType<Parent>
        {
            protected override void Configure(ISortInputTypeDescriptor<Parent> descriptor)
            {
                descriptor.SortableObject(m => m.Model);
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
                    .Resolver(c => c.Parent<Model>().Id);
            }
        }

        public class ParentType : ObjectType<Parent>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Parent> descriptor)
            {
                descriptor.Field(t => t.Model)
                    .Type<ModelType>();
            }
        }
        public class Parent
        {
            public Model Model { get; set; }
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
