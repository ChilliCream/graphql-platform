using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class ConnectionTypeTests
        : TypeTestBase
    {
        [Fact]
        public void CheckThatNameIsCorrect()
        {
            // arrange
            // act
            ConnectionType<StringType> type =
                CreateType(new ConnectionType<StringType>());

            // assert
            Assert.Equal("StringConnection", type.Name);
        }

        [Fact]
        public void CheckFieldsAreCorrect()
        {
            // arrange
            // act
            ConnectionType<StringType> type = CreateType(
                new ConnectionType<StringType>());

            // assert
            Assert.Collection(
                type.Fields.Where(t => !t.IsIntrospectionField).OrderBy(t => t.Name),
                t =>
                {
                    Assert.Equal("edges", t.Name);
                    Assert.IsType<ListType>(t.Type);
                    Assert.IsType<NonNullType>(((ListType)t.Type).ElementType);
                    Assert.IsType<EdgeType<StringType>>(
                        ((NonNullType)((ListType)t.Type).ElementType).Type);
                },
                t =>
                {
                    Assert.Equal("nodes", t.Name);
                    Assert.IsType<StringType>(
                        Assert.IsType<ListType>(t.Type).ElementType);
                },
                t =>
                {
                    Assert.Equal("pageInfo", t.Name);
                    Assert.IsType<NonNullType>(t.Type);
                    Assert.IsType<PageInfoType>(((NonNullType)t.Type).Type);
                });
        }

        [Fact]
        public async Task ExecuteQueryWithPaging()
        {
            // arrange
            ISchema schema = Schema.Create(
                c => c.RegisterQueryType<QueryType>());
            IQueryExecutor executor = schema.MakeExecutable();

            string query = @"
            {
                s(last:2)
                {
                    edges {
                        cursor
                        node
                    }
                    pageInfo
                    {
                        hasNextPage
                    }
                    totalCount
                }
            }
            ";

            // act
            IExecutionResult result = await executor
                .ExecuteAsync(QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task UsePaging_WithNonNull_ElementType()
        {
            // arrange
            ISchema schema = Schema.Create(
                c => c.RegisterQueryType<QueryType2>());
            IQueryExecutor executor = schema.MakeExecutable();

            string query = @"
            {
                s(last:2)
                {
                    edges {
                        cursor
                        node
                    }
                    nodes
                    pageInfo
                    {
                        hasNextPage
                    }
                    totalCount
                }
            }
            ";

            // act
            IExecutionResult result = await executor
                .ExecuteAsync(QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create());

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task UsePaging_WithComplexType()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddType<FooType>()
                .AddQueryType<QueryType3>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            string query = @"
            {
                s
                {
                    bar {
                        edges {
                            cursor
                            node
                        }
                        pageInfo
                        {
                            hasNextPage
                        }
                        totalCount
                    }
                }
            }
            ";

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void InferSchemaWithAttributesCorrectly()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryWithPagingAttribute>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public async Task UsePagingAttribute_InMemory_Collection()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithPagingAttribute>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            string query = @"
            {
                collection {
                    edges {
                        cursor
                        node
                    }
                    pageInfo
                    {
                        hasNextPage
                    }
                    totalCount
                }
            }
            ";

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task UsePagingAttribute_InMemory_Queryable()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithPagingAttribute>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            string query = @"
            {
                queryable {
                    edges {
                        cursor
                        node
                    }
                    pageInfo
                    {
                        hasNextPage
                    }
                    totalCount
                }
            }
            ";

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task UsePagingAttribute_InMemory_Enumerable()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithPagingAttribute>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            string query = @"
            {
                enumerable {
                    edges {
                        cursor
                        node
                    }
                    pageInfo
                    {
                        hasNextPage
                    }
                    totalCount
                }
            }
            ";

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ConnectionType_Without_Paging_Middleware()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithPagingAttribute>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            string query = @"
            {
                connectionOfString {
                    edges {
                        cursor
                        node
                    }
                    pageInfo
                    {
                        hasNextPage
                    }
                    totalCount
                }
            }
            ";

            // act
            IExecutionResult result = await executor.ExecuteAsync(query);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task UsePagingAttribute_With_Injected_ConnectionResolver()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithPagingAttribute>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            string query = @"
            {
                enumerable {
                    edges {
                        cursor
                        node
                    }
                    pageInfo
                    {
                        hasNextPage
                    }
                    totalCount
                }
            }";

            IServiceProvider services = new ServiceCollection()
                .AddSingleton<IConnectionResolver<IEnumerable<string>>, ConnectionOfString>()
                .BuildServiceProvider();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(query)
                .SetServices(services)
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        public class QueryType
            : ObjectType
        {
            private readonly List<string> _source =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" };

            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("s")
                    .UsePaging<StringType>()
                    .Resolver(ctx => _source);
            }
        }

        public class QueryType2
            : ObjectType
        {
            private readonly List<string> _source =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" };

            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("s")
                    .UsePaging<NonNullType<StringType>>()
                    .Resolver(ctx => _source);
            }
        }

        public class QueryType3
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("s")
                    .Resolver(ctx => new Foo());
            }
        }

        public class Query
        {
            public ICollection<string> Strings { get; } =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" };
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Interface<FooInterfaceType>();
                descriptor.Field<ICollection<string>>(t => t.Bar).UsePaging<StringType>();
            }
        }

        public class FooInterfaceType
            : InterfaceType
        {
            protected override void Configure(
                IInterfaceTypeDescriptor descriptor)
            {
                descriptor.Name("IFoo");
                descriptor.Field("bar")
                    .UsePaging<StringType>();
            }
        }

        public class Foo
        {
            public ICollection<string> Bar { get; } =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" };
        }

        public class QueryWithPagingAttribute
        {
            [UsePaging]
            public ICollection<string> Collection { get; } =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" };

            [UsePaging]
            public IQueryable<string> Queryable { get; } =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" }.AsQueryable();

            [UsePaging]
            public IEnumerable<string> Enumerable { get; } =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" }.AsQueryable();

            [GraphQLType(typeof(ConnectionWithCountType<StringType>))]
            public Connection<string> ConnectionOfString(
                int? first = null, 
                int? last = null, 
                string? after = null, 
                string? before = null) =>
                new Connection<string>(
                    new PageInfo(false, false, "foo", "foo", 1),
                    new List<Edge<string>> { new Edge<string>("abc", "foo") });
        }

        public class ConnectionOfString : IConnectionResolver<IEnumerable<string>>
        {
            public ValueTask<IConnection> ResolveAsync(
                IMiddlewareContext context,
                IEnumerable<string> source,
                ConnectionArguments arguments = default,
                bool withTotalCount = false,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<IConnection>(new Connection<string>(
                    new PageInfo(false, false, "foo", "foo", 1),
                    new List<Edge<string>> { new Edge<string>("abc", "foo") }));
            }

            public ValueTask<IConnection> ResolveAsync(
                IMiddlewareContext context,
                object source,
                ConnectionArguments arguments = default,
                bool withTotalCount = false,
                CancellationToken cancellationToken = default) =>
                ResolveAsync(
                    context, 
                    (IEnumerable<string>)source, 
                    arguments, 
                    withTotalCount, 
                    cancellationToken);
        }
    }
}
