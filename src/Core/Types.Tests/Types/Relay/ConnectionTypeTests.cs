using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
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
            ConnectionType<StringType> type = CreateType(new ConnectionType<StringType>());

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
                .ExecuteAsync(new QueryRequest(query));

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

        public class Query
        {
            public ICollection<string> Strings { get; } =
                new List<string> { "a", "b", "c", "d", "e", "f", "g" };
        }
    }
}
