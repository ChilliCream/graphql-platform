using System;
using System.Linq;
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

        /// <summary>Match snapshot by adding extensions to an existing fullname</summary>
        /// <remarks>
        /// This is the only method that will completely ignore the Fullname cached in the AsyncLocal storage.
        /// Use this when you want to check multiple snapshots into one async test that does not trigger an AsyncLocal clear)
        /// </remarks>
        public static async Task<IExecutionResult> MatchSnapshotAsync(
            this Task<IExecutionResult> task,
            SnapshotFullName snapshotFullName,
            params object[] snapshotNameExtensions)
        {
            IExecutionResult result = await task;
            result.ToJson(true).MatchSnapshot(
                snapshotFullName.ApplyExtensions(snapshotNameExtensions));
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

        /// <summary>Create a new fullname by applying existings to an existing one</summary>
        public static SnapshotFullName ApplyExtensions(
            this SnapshotFullName fullName,
            params object[] snapshotNameExtensions)
        {
            var additions = SnapshotNameExtension.Create(snapshotNameExtensions).ToParamsString();
            var oldExtension = System.IO.Path.GetExtension(fullName.Filename);
            var oldBasename = System.IO.Path.GetFileNameWithoutExtension(fullName.Filename);
            return new SnapshotFullName(oldBasename + additions + oldExtension, fullName.FolderPath);
        }
    }
}
