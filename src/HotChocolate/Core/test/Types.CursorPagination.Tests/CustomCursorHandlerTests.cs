using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Pagination
{
    public class CustomCursorHandlerTests
    {
        [Fact]
        public void Infer_Schema_Correctly_When_Connection_IsUsed()
        {
            SchemaBuilder.New()
                .AddQueryType<Query>()
                .Create()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public async Task Use_Resolver_Result_If_It_Is_A_Page()
        {
            // arrange
            Snapshot.FullName();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ items { nodes } }")
                    .Create();

            // act
            // assert
            await SchemaBuilder.New()
                .AddQueryType<Query>()
                .Create()
                .MakeExecutable()
                .ExecuteAsync(request)
                .MatchSnapshotAsync();
        }

        public class Query
        {
            [UsePaging]
            public Connection<string> GetItems(int first, string after, int last, string before)
            {
                return new Connection<string>(
                    new[] { new Edge<string>("hello", "abc") },
                    new ConnectionPageInfo(false, false, "abc", "abc", 2000),
                    ct => throw new NotImplementedException());
            }
        }
    }
}
