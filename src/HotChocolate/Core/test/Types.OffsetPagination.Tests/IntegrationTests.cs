using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class IntegrationTests
    {
        [Fact]
        public async Task Simple_StringList_Schema()
        {
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            executor.Schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task Simple_StringList_Default_Items()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_Take_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_Take_2_Skip_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters(take: 2 skip: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_Global_DefaultItem_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .SetPagingSettings(new PagingSettings { DefaultPageSize = 2 })
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        public class QueryType : ObjectType<Query>
        {
            protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.Letters).UseOffsetPaging(typeof(StringType));
            }
        }


        public class Query
        {
            public string[] Letters => new[]
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g",
                "h",
                "i",
                "j",
                "k",
                "l"
            };
        }
    }
}
