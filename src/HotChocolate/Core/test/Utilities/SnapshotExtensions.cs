using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter;
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

        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result,
            string snapshotNameExtension)
        {
            result.ToJson(true).MatchSnapshot(
                SnapshotNameExtension.Create(snapshotNameExtension));
            return result;
        }

        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result,
            params string[] snapshotNameExtensions)
        {
            result.ToJson(true).MatchSnapshot(
                SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result,
            params object[] snapshotNameExtensions)
        {
            result.ToJson(true).MatchSnapshot(
                SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            string snapshotNameExtension)
        {
            IExecutionResult result = await task;
            result.ToJson(true).MatchSnapshot(
                SnapshotNameExtension.Create(snapshotNameExtension));
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            params string[] snapshotNameExtensions)
        {
            IExecutionResult result = await task;
            result.ToJson(true).MatchSnapshot(
                SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            params object[] snapshotNameExtensions)
        {
            IExecutionResult result = await task;
            result.ToJson(true).MatchSnapshot(
                SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }
    }
}
