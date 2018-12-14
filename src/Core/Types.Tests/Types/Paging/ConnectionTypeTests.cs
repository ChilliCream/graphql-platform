using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
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
                new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("s")
                    .Argument("first", a => a.Type<IntType>())
                    .Argument("after", a => a.Type<StringType>())
                    .Argument("last", a => a.Type<IntType>())
                    .Argument("before", a => a.Type<StringType>())
                    .Type<ConnectionType<StringType>>()
                    .Resolver(ctx => new QueryableConnection<string>(
                        _source.AsQueryable(),
                        new PagingDetails
                        {
                            First = ctx.Argument<int?>("first"),
                            After = ctx.Argument<string>("after"),
                            Last = ctx.Argument<int?>("last"),
                            Before = ctx.Argument<string>("before")
                        }));
            }
        }
    }
}
