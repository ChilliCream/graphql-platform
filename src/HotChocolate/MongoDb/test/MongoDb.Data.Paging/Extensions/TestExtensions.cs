using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.MongoDb.Data.Paging
{
    public static class TestExtensions
    {
        public static async Task MatchDocumentSnapshotAsync(
            this Task<IExecutionResult?> resultTask,
            string snapshotName = "")
        {
            IExecutionResult? result = await resultTask.ConfigureAwait(false);

            if (result is { })
            {
                result.MatchSnapshot(snapshotName);
                if (result.ContextData is { } &&
                    result.ContextData.TryGetValue("query", out object? queryResult) &&
                    queryResult is string queryString &&
                    !string.IsNullOrWhiteSpace(queryString))
                {
                    queryString.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_query"));
                }
            }
        }
    }
}
