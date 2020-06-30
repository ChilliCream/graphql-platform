using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using MongoDB.Bson;
using MongoDB.Driver;
using Snapshooter.Xunit;
using Squadron;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Types.Filters
{
    public class MongoFilterTests
        : IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public MongoFilterTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public async Task GetItems_NoFilter_AllItems_Are_Returned()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ items { foo } }",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Model>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Model> collection = database.GetCollection<Model>("col");
                        collection.InsertMany(new[]
                        {
                            new Model { Foo = "abc", Bar = 1, Baz = true },
                            new Model { Foo = "def", Bar = 2, Baz = false },
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GetItems_EqualsFilter_FirstItems_Is_Returned()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ items(where: { foo: \"abc\" }) { foo } }",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Model>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Model> collection = database.GetCollection<Model>("col");
                        collection.InsertMany(new[]
                        {
                            new Model { Foo = "abc", Bar = 1, Baz = true },
                            new Model { Foo = "def", Bar = 2, Baz = false },
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GetItems_ObjectEqualsFilter_FirstItems_Is_Returned()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ items(where: { nested:{ nested: { foo: \"abc\" " +
                "} } }) { nested { nested { foo } } } }",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Model>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Model> collection = database.GetCollection<Model>("col");
                        collection.InsertMany(new[]
                        {
                            new Model
                            {
                                Nested = null
                            },
                            new Model
                            {
                                Nested = new Model
                                {
                                    Nested = new Model
                                    {
                                        Foo = "abc",
                                        Bar = 1,
                                        Baz = true
                                    }
                                }
                            },
                            new Model
                            {
                                Nested = new Model
                                {
                                    Nested= new Model
                                    {
                                        Foo = "def",
                                        Bar = 2,
                                        Baz = false
                                    }
                                }
                            },
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task GetItems_With_Paging_EqualsFilter_FirstItems_Is_Returned()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ paging(where: { foo: \"abc\" }) { nodes { foo } } }",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Model>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Model> collection = database.GetCollection<Model>("col");
                        collection.InsertMany(new[]
                        {
                            new Model { Foo = "abc", Bar = 1, Baz = true },
                            new Model { Foo = "def", Bar = 2, Baz = false },
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Boolean_Filter_Equals()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ paging(where: { baz: true }) { nodes { foo } } }",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Model>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Model> collection = database.GetCollection<Model>("col");
                        collection.InsertMany(new[]
                        {
                            new Model { Foo = "abc", Bar = 1, Baz = true },
                            new Model { Foo = "def", Bar = 2, Baz = false },
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Boolean_Filter_Not_Equals()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ paging(where: { baz_not: false }) { nodes { foo } } }",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Model>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Model> collection = database.GetCollection<Model>("col");
                        collection.InsertMany(new[]
                        {
                            new Model { Foo = "abc", Bar = 1, Baz = true },
                            new Model { Foo = "def", Bar = 2, Baz = false },
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task DateTimeType_GreaterThan_Filter()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ items(where: { time_gt: \"2001-01-01\" }) { time } }",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Model>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Model> collection = database.GetCollection<Model>("col");
                        collection.InsertMany(new[]
                        {
                            new Model 
                            { 
                                Time = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc) 
                            },
                            new Model 
                            { 
                                Time = new DateTime(2016, 1, 1, 1, 1, 1, DateTimeKind.Utc) 
                            },
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task DateType_GreaterThan_Filter()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ items(where: { date_gt: \"2001-01-01\" }) { date } }",
                configure: c => c
                    .AddQueryType<QueryType>()
                    .Services
                    .AddSingleton<IMongoCollection<Model>>(sp =>
                    {
                        IMongoDatabase database = _mongoResource.CreateDatabase();

                        IMongoCollection<Model> collection = database.GetCollection<Model>("col");
                        collection.InsertMany(new[]
                        {
                            new Model 
                            {
                                Date = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc).Date 
                            },
                            new Model 
                            { 
                                Date = new DateTime(2016, 1, 1, 1, 1, 1, DateTimeKind.Utc).Date 
                            },
                        });
                        return collection;
                    }))
                .MatchSnapshotAsync();
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

                descriptor.Field(t => t.Time)
                    .Type<NonNullType<DateTimeType>>();

                descriptor.Field(t => t.Date)
                    .Type<NonNullType<DateType>>();
            }
        }

        public class Model
        {
            public ObjectId Id { get; set; }

            public string Foo { get; set; }

            public int Bar { get; set; }

            public bool Baz { get; set; }

            public Model Nested { get; set; }

            [GraphQLType(typeof(NonNullType<DateTimeType>))]
            public DateTime Time { get; set; }

            [GraphQLType(typeof(NonNullType<DateType>))]
            public DateTime Date { get; set; }
        }
    }
}
