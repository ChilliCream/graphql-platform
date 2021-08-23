using System;
using MongoDB.Bson;
using Xunit;
using Squadron;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using HotChocolate.Execution;
using Snapshooter.Xunit;

namespace HotChocolate.Types.Filters
{
    [Obsolete]
    public class ArrayFilterTests
        : IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public ArrayFilterTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public async Task Array_Filter_On_Scalar_Types()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoCollection<Foo>>(sp =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    return database.GetCollection<Foo>("col");
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .BindRuntimeType<ObjectId, IdType>()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IMongoCollection<Foo> collection = services.GetRequiredService<IMongoCollection<Foo>>();

            await collection.InsertOneAsync(new Foo
            {
                BarCollection = new List<string> { "a", "b", "c" },
                BazCollection = new List<Baz> { new Baz { Quux = "a" }, new Baz { Quux = "b" } },
                Bars = new[] { "d", "e", "f" },
                Bazs = new[] { new Baz { Quux = "c" }, new Baz { Quux = "d" } },
                Quux = "abc"
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(services)
                .BindClrType<ObjectId, IdType>()
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "{" +
                    "foos(where: { bars_some: { element: \"e\" } }) { bars } " +
                    "}")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Array_Filter_On_Objects_Types()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoCollection<Foo>>(sp =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    return database.GetCollection<Foo>("col");
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .BindRuntimeType<ObjectId, IdType>()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IMongoCollection<Foo> collection = services.GetRequiredService<IMongoCollection<Foo>>();

            await collection.InsertOneAsync(new Foo
            {
                BarCollection = new List<string> { "a", "b", "c" },
                BazCollection = new List<Baz> { new Baz { Quux = "a" }, new Baz { Quux = "b" } },
                Bars = new[] { "d", "e", "f" },
                Bazs = new[] { new Baz { Quux = "c" }, new Baz { Quux = "d" } },
                Quux = "abc"
            });

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "{" +
                    "a: foos(where: { bazs_some: { quux: \"c\" } }) { bars } " +
                    "}")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Collection_Filter_On_Scalar_Types()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoCollection<Foo>>(sp =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    return database.GetCollection<Foo>("col");
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .BindRuntimeType<ObjectId, IdType>()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IMongoCollection<Foo> collection = services.GetRequiredService<IMongoCollection<Foo>>();

            await collection.InsertOneAsync(new Foo
            {
                BarCollection = new List<string> { "a", "b", "c" },
                BazCollection = new List<Baz> { new Baz { Quux = "a" }, new Baz { Quux = "b" } },
                Bars = new[] { "d", "e", "f" },
                Bazs = new[] { new Baz { Quux = "c" }, new Baz { Quux = "d" } },
                Quux = "abc"
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(services)
                .BindClrType<ObjectId, IdType>()
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "{" +
                    "foos(where: { barCollection_some: { element: \"b\" } }) { bars } " +
                    "}")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Collection_Filter_On_Objects_Types()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoCollection<Foo>>(sp =>
                {
                    IMongoDatabase database = _mongoResource.CreateDatabase();
                    return database.GetCollection<Foo>("col");
                })
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .BindRuntimeType<ObjectId, IdType>()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IMongoCollection<Foo> collection = services.GetRequiredService<IMongoCollection<Foo>>();

            await collection.InsertOneAsync(new Foo
            {
                BarCollection = new List<string> { "a", "b", "c" },
                BazCollection = new List<Baz> { new Baz { Quux = "a" }, new Baz { Quux = "b" } },
                Bars = new[] { "d", "e", "f" },
                Bazs = new[] { new Baz { Quux = "c" }, new Baz { Quux = "d" } },
                Quux = "abc"
            });

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(services)
                .BindClrType<ObjectId, IdType>()
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "{" +
                    "a: foos(where: { bazCollection_some: { quux: \"a\" } }) { bars } " +
                    "}")
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
                descriptor.Field("foos")
                    .Type<ListType<ObjectType<Foo>>>()
                    .UseFiltering<FilterInputType<Foo>>()
                    .Resolver(ctx => ctx.Service<IMongoCollection<Foo>>().AsQueryable());
            }
        }

        public class Foo
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public ObjectId Id { get; set; }
            public string[] Bars { get; set; }
            public Baz[] Bazs { get; set; }
            public ICollection<string> BarCollection { get; set; }
            public ICollection<Baz> BazCollection { get; set; }
            public string Quux { get; set; }
        }

        public class Baz
        {
            public string Quux { get; set; }
        }
    }
}
