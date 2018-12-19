using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Types.Paging
{
    public class ConnectionTypeTests
    {
        [Fact]
        public void CheckThatNameIsCorrect()
        {
            // arrange
            // act
            var type = new ConnectionType<StringType>();

            // assert
            Assert.Equal("StringConnection", type.Name);
        }

        [Fact]
        public void CheckFieldsAreCorrect()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var type = new ConnectionType<StringType>();

            // assert
            INeedsInitialization init = type;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), type, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Collection(type.Fields.Where(t => !t.IsIntrospectionField),
                t =>
                {
                    Assert.Equal("pageInfo", t.Name);
                    Assert.IsType<NonNullType>(t.Type);
                    Assert.IsType<PageInfoType>(((NonNullType)t.Type).Type);
                },
                t =>
                {
                    Assert.Equal("edges", t.Name);
                    Assert.IsType<ListType>(t.Type);
                    Assert.IsType<NonNullType>(((ListType)t.Type).ElementType);
                    Assert.IsType<EdgeType<StringType>>(
                        ((NonNullType)((ListType)t.Type).ElementType).Type);
                });
        }

        [Fact]
        public async Task ExecuteQueryWithPaging()
        {
            // arrange
            ISchema schema = Schema.Create(
                c => c.RegisterQueryType<QueryType>());
            IQueryExecuter executer = schema.MakeExecutable();

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
                }
            }
            ";

            // act
            IExecutionResult result = await executer
                .ExecuteAsync(new QueryRequest(query));

            // assert
            result.Snapshot();
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
                    .UsePaging<StringType, string>()
                    .Resolver(ctx => _source);
            }
        }

        public class Query
        {
            public ICollection<string> Strings { get; } =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" };
        }
    }
}
