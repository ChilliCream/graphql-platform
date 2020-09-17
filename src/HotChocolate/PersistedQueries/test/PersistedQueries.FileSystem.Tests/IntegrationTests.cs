using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using IO = System.IO;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.PersistedQueries.FileSystem
{
    public class IntegrationTests
    {
        [Fact]
        public async Task ExecutePersistedQuery()
        {
            // arrange
            var queryId = Guid.NewGuid().ToString("N");
            var cacheDirectory = IO.Path.GetTempPath();
            var cachedQuery = IO.Path.Combine(cacheDirectory, queryId + ".graphql");

            await File.WriteAllTextAsync(cachedQuery, "{ __typename }");

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                    .AddFileSystemQueryStorage(cacheDirectory)
                    .UsePersistedQueryPipeline()
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

            // assert
            result.MatchSnapshot();
        }
    }
}
