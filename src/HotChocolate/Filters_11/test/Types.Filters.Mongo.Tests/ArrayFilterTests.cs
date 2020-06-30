using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using MongoDB.Bson;
using MongoDB.Driver;
using Snapshooter.Xunit;
using Squadron;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Types.Filters
{
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
            Snapshot.FullName();
            await ExpectValid(
                "{" +
                "foos(where: { bars_some: { element: \"e\" } }) { bars } " +
                "}",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Foo>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Foo> collection = database.GetCollection<Foo>("col");
                        collection.InsertOne(new Foo
                        {
                            BarCollection = new List<string> { "a", "b", "c" },
                            BazCollection = new List<Baz> { new Baz { Quux = "a" }, new Baz { Quux = "b" } },
                            Bars = new[] { "d", "e", "f" },
                            Bazs = new[] { new Baz { Quux = "c" }, new Baz { Quux = "d" } },
                            Quux = "abc"
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Array_Filter_On_Objects_Types()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IMongoCollection<Foo>>(sp =>
            {
                IMongoDatabase database = _mongoResource.CreateDatabase();
                return database.GetCollection<Foo>("col");
            });

            IServiceProvider services = serviceCollection.BuildServiceProvider();
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

            IRequestExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "{" +
                    "a: foos(where: { bazs_some: { quux: \"c\" } }) { bars } " +
                    "}")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Collection_Filter_On_Scalar_Types()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IMongoCollection<Foo>>(sp =>
            {
                IMongoDatabase database = _mongoResource.CreateDatabase();
                return database.GetCollection<Foo>("col");
            });

            IServiceProvider services = serviceCollection.BuildServiceProvider();
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

            IRequestExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "{" +
                    "foos(where: { barCollection_some: { element: \"b\" } }) { bars } " +
                    "}")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Collection_Filter_On_Objects_Types()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IMongoCollection<Foo>>(sp =>
            {
                IMongoDatabase database = _mongoResource.CreateDatabase();
                return database.GetCollection<Foo>("col");
            });

            IServiceProvider services = serviceCollection.BuildServiceProvider();
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

            IRequestExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "{" +
                    "a: foos(where: { bazCollection_some: { quux: \"a\" } }) { bars } " +
                    "}")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.ToJson().MatchSnapshot();
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
