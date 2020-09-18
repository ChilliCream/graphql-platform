using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;
using IO = System.IO;

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
                    .UseRequest(n => async c =>
                    {
                        await n(c);

                        if (c.IsPersistedDocument && c.Result is IQueryResult r)
                        {
                            c.Result = QueryResultBuilder
                                .FromResult(r)
                                .SetExtension("persistedDocument", true)
                                .Create();
                        }
                    })
                    .UsePersistedQueryPipeline()
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: queryId));

            // assert
            File.Delete(cachedQuery);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecutePersistedQuery_NotFound()
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
                    .UseRequest(n => async c =>
                    {
                        await n(c);

                        if (c.IsPersistedDocument && c.Result is IQueryResult r)
                        {
                            c.Result = QueryResultBuilder
                                .FromResult(r)
                                .SetExtension("persistedDocument", true)
                                .Create();
                        }
                    })
                    .UsePersistedQueryPipeline()
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(new QueryRequest(queryId: "does_not_exist"));

            // assert
            File.Delete(cachedQuery);
            result.MatchSnapshot();
        }
    }
}
