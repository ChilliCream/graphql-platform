using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Mongo;
using Snapshooter.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;
using HotChocolate.Resolvers;
using MongoDB.Driver;
using MongoDB.Bson;

namespace HotChocolate.Types.Filters
{
    public class FilterTests
        : IClassFixture<MongoResource>
    {
        private readonly ServiceProvider _sp;

        public FilterTests(MongoResource mongoResource)
        {
            IMongoCollection<Foo> collection = mongoResource.CreateCollection<Foo>("a");
            collection.InsertMany(new Foo[]
            {
                new Foo { Bar = "aa", Baz = 1, Qux = 1 },
                new Foo { Bar = "ba", Baz = 1 },
                new Foo { Bar = "ca", Baz = 2 },
                new Foo { Bar = "ab", Baz = 2 },
                new Foo { Bar = "ac", Baz = 2 },
                new Foo { Bar = "ad", Baz = 2 },
                new Foo { Bar = null, Baz = 0 }
            });
            IMongoCollection<FooObject> collectionObject =
                mongoResource.CreateCollection<FooObject>("b");
            collectionObject.InsertMany(new FooObject[]
            {
                new FooObject { FooNested = new FooNested { Bar = "a" }},
                new FooObject { FooNested = new FooNested { Bar = "b" }},
            });

            _sp = new ServiceCollection()
                .AddSingleton(collection)
                .AddSingleton(collectionObject)
                .BuildServiceProvider();
        }

        [Fact]
        public void Create_Schema_With_FilterType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddQueryType<QueryType>()
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Schema_With_FilterType_With_Fluent_API()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddQueryType<Query>(d =>
                    d.Field(m => m.GetFoos(default!, default!))
                        .UseFiltering<Foo>(f =>
                            f.BindFieldsExplicitly()
                                .Filter(m => m.Bar)
                                .BindFiltersExplicitly()
                                .AllowEquals()))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddQueryType<QueryType>()
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_starts_with: \"a\" }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_With_Variables()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddQueryType<QueryType>()
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query filter($a: String) {
                        foos(where: { bar_starts_with: $a }) {
                            bar
                        }
                    }")
                .SetVariableValue("a", "a")
                .Create();

            // act
            IExecutionResult result = executor.Execute(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_As_Variable()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddQueryType<QueryType>()
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query filter($a: FooFilter) {
                        foos(where: $a) {
                            bar
                        }
                    }")
                .SetVariableValue("a", new Dictionary<string, object>
                {
                    { "bar_starts_with", "a" }
                })
                .Create();

            // act
            IExecutionResult result = executor.Execute(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Is_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddQueryType<QueryType>()
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Execute_ObjectStringEqualWith()
        {
            // arrange

            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType(x =>
                   x.Field("list")
                   .Type<ListType<ObjectType<FooObject>>>()
                   .Resolver(
                        context =>
                        {
                            if (context.LocalContextData.TryGetValue(
                                    nameof(FilterDefinition<BsonDocument>), out object val) &&
                                val is BsonDocument filterDefinition)
                            {
                                return context.Service<IMongoCollection<FooObject>>()
                                    .Find(filterDefinition)
                                    .ToListAsync().ConfigureAwait(false);
                            }
                            throw new NotSupportedException();
                        }
                    )
                   .UseFiltering()
                    )
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ list(where: { fooNested: { bar: \"a\" } }) { fooNested { bar } } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Infer_Filter_From_Field()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_starts_with: \"a\" }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar: null }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Not_Equals_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_not: null }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_In()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_in: [ \"aa\" \"ab\" ] }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Comparable_In()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { baz_in: [ 1 0 ] }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Nullable_Equals_1()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { qux: 1 }) { bar qux } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Nullable_Equals_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { qux: null }) { bar qux } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_And()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { AND: [ { bar: \"aa\" } { bar: \"ba\" } ] })" +
                " { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_Or()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<Query>(
                    d => d.Field(t => t.GetFoos(default!, default!)).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { OR: [ { bar: \"aa\" } { bar: \"ba\" } ] })" +
                " { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Execute_DateTime_Filter()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<QueryFooDateTime>(d => d
                    .Name("Query")
                    .Field(y => y.Foo)
                    .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ foo(where: { foo_gte: \"2019-06-01\"}) { foo } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Execute_DateTime_Filter_With_Variables()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddServices(_sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseMongoVisitor().UseDefault()))
                .AddQueryType<QueryFooDateTime>(d => d
                    .Name("Query")
                    .Field(y => y.Foo)
                    .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "query TestQuery($where: FooDateTimeFilter) {" +
                    "foo(where: $where) { foo } }")
                .SetVariableValue("where", new Dictionary<string, object>
                {
                    { "foo_gte", "2019-06-01" }
                })
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request).ConfigureAwait(false);

            // assert
            result.MatchSnapshot();
        }

        public class FooDateTime
        {
            public DateTime Foo { get; set; }
        }

        public class QueryFooDateTime
        {
            public IEnumerable<FooDateTime> Foo { get; set; } = new List<FooDateTime>
            {
                new FooDateTime { Foo = new DateTime(2020,01,01, 18, 0, 0, DateTimeKind.Utc) },
                new FooDateTime { Foo = new DateTime(2018,01,01, 18, 0, 0, DateTimeKind.Utc) }
            };
        }

        public class FooObject
        {
            public FooNested FooNested { get; set; }
        }
        public class FooNested
        {
            public string Bar { get; set; }
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.GetFoos(default!, default!)).UseFiltering<Foo>();
            }
        }

        public class Query
        {
            public async Task<List<Foo>> GetFoos(
                IResolverContext context,
                [Service] IMongoCollection<Foo> collection)
            {
                if (context.LocalContextData.TryGetValue(
                        nameof(FilterDefinition<BsonDocument>), out object val) &&
                    val is BsonDocument filterDefinition)
                {
                    return await collection.Find(filterDefinition)
                        .ToListAsync().ConfigureAwait(false);
                }
                return await collection.Find(x => true).ToListAsync().ConfigureAwait(false);
            }
        }

        public class Foo
        {
            public Guid Id { get; set; }

            public string Bar { get; set; }

            [GraphQLType(typeof(NonNullType<IntType>))]
            public long Baz { get; set; }

            [GraphQLType(typeof(IntType))]
            public int? Qux { get; set; }
        }
    }
}
