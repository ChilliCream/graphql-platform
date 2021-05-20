using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Tests
{
    public static class SnapshotExtensions
    {
        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result)
        {
            result.ToJson().MatchSnapshot();
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task)
        {
            IExecutionResult result = await task;
            string json = await task.ToJsonAsync();
            json.MatchSnapshot();
            return result;
        }

        public static async Task<ISchema> MatchSnapshotAsync(
            this Task<ISchema> task)
        {
            ISchema result = await task;
            result.Print().MatchSnapshot();
            return result;
        }

        public static async Task<string> MatchSnapshotAsync(
            this Task<string> task)
        {
            string result = await task;
            result.MatchSnapshot();
            return result;
        }

        public static async ValueTask<string> MatchSnapshotAsync(
            this ValueTask<string> task)
        {
            string result = await task;
            result.MatchSnapshot();
            return result;
        }

        public static async ValueTask<ISchema> MatchSnapshotAsync(
            this ValueTask<ISchema> task)
        {
            ISchema result = await task;
            result.Print().MatchSnapshot();
            return result;
        }

        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result,
            string snapshotNameExtension)
        {
            result.ToJson().MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtension));
            return result;
        }

        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result,
            params string[] snapshotNameExtensions)
        {
            result.ToJson().MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result,
            params object[] snapshotNameExtensions)
        {
            result.ToJson().MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            string snapshotNameExtension)
        {
            IExecutionResult result = await task;
            string json = await task.ToJsonAsync();
            json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtension));
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            params string[] snapshotNameExtensions)
        {
            IExecutionResult result = await task;
            string json = await task.ToJsonAsync();
            json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            params object[] snapshotNameExtensions)
        {
            IExecutionResult result = await task;
            string json = await task.ToJsonAsync();
            json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static async Task<string> ToJsonAsync(this Task<IExecutionResult> task)
        {
            IExecutionResult result = await task;
            return await result.ToJsonAsync();
        }
    }
}
