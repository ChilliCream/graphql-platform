using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;

namespace HotChocolate.Tests
{
    public static class SnapshotExtensions
    {
        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result)
        {
            result.ToJson(true).MatchSnapshot();
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task)
        {
            IExecutionResult result = await task;
            result.ToJson(true).MatchSnapshot();
            return result;
        }
    }
}
