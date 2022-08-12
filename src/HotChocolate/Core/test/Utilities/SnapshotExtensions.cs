using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Tests
{
    public static class SnapshotExtensions
    {
        private static readonly JsonArrayResponseStreamFormatter _formatter = new(true);

        public static IExecutionResult MatchSnapshot(
            this IExecutionResult result)
        {
            result.ToJson().MatchSnapshot();
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this IExecutionResult result,
            CancellationToken cancellationToken = default)
        {
            if (result is IQueryResult q)
            {
                q.ToJson().MatchSnapshot();
                return result;
            }

            if (result is IResponseStream responseStream)
            {
                await using var memoryStream = new MemoryStream();
                await _formatter.FormatAsync(responseStream, memoryStream, cancellationToken);
                Encoding.UTF8.GetString(memoryStream.ToArray()).MatchSnapshot();
                return result;
            }

            throw new NotSupportedException($"{result.GetType().FullName} is not supported.");
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task)
        {
            IExecutionResult result = await task;
            var json = await task.ToJsonAsync();
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
            var result = await task;
            result.MatchSnapshot();
            return result;
        }

        public static async ValueTask<string> MatchSnapshotAsync(
            this ValueTask<string> task)
        {
            var result = await task;
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
            var json = await task.ToJsonAsync();
            json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtension));
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            params string[] snapshotNameExtensions)
        {
            IExecutionResult result = await task;
            var json = await task.ToJsonAsync();
            json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            params object[] snapshotNameExtensions)
        {
            IExecutionResult result = await task;
            var json = await task.ToJsonAsync();
            json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtensions));
            return result;
        }

        public static async Task<string> ToJsonAsync(this Task<IExecutionResult> task)
        {
            IExecutionResult result = await task;
            return result.ToJson();
        }

        public static void MatchSnapshot(this GraphQLException ex)
        {
            QueryResultBuilder.CreateError(ex.Errors).MatchSnapshot();
        }
    }
}
