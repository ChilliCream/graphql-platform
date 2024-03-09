using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Tests;

public static class SnapshotExtensions
{
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
        if (result is IOperationResult q)
        {
            q.ToJson().MatchSnapshot();
            return result;
        }

        if (result is IResponseStream responseStream)
        {
            await using var memoryStream = new MemoryStream();
            await using var jsonWriter = new Utf8JsonWriter(
                memoryStream,
                new JsonWriterOptions { Indented = true, });

            jsonWriter.WriteStartArray();

            await foreach (var queryResult in responseStream.ReadResultsAsync()
                .WithCancellation(cancellationToken))
            {
                jsonWriter.WriteRawValue(queryResult.ToJson(), true);
            }

            jsonWriter.WriteEndArray();
            await jsonWriter.FlushAsync(cancellationToken);

            Encoding.UTF8.GetString(memoryStream.ToArray()).MatchSnapshot();
            return result;
        }

        throw new NotSupportedException($"{result.GetType().FullName} is not supported.");
    }

    public static async Task<IExecutionResult> MatchSnapshotAsync(
        this Task<IExecutionResult> task)
    {
        var result = await task;
        var json = await task.ToJsonAsync();
        json.MatchSnapshot();
        return result;
    }

    public static async Task<ISchema> MatchSnapshotAsync(
        this Task<ISchema> task)
    {
        var result = await task;
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
        var result = await task;
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
        var result = await task;
        var json = await task.ToJsonAsync();
        json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtension));
        return result;
    }

    public static async Task<IExecutionResult> MatchSnapshotAsync(
        this Task<IExecutionResult> task,
        params string[] snapshotNameExtensions)
    {
        var result = await task;
        var json = await task.ToJsonAsync();
        json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtensions));
        return result;
    }

    public static async Task<IExecutionResult> MatchSnapshotAsync(
        this Task<IExecutionResult> task,
        params object[] snapshotNameExtensions)
    {
        var result = await task;
        var json = await task.ToJsonAsync();
        json.MatchSnapshot(SnapshotNameExtension.Create(snapshotNameExtensions));
        return result;
    }

    public static async Task<string> ToJsonAsync(this Task<IExecutionResult> task)
    {
        var result = await task;
        return result.ToJson();
    }

    public static void MatchSnapshot(this GraphQLException ex)
    {
        OperationResultBuilder.CreateError(ex.Errors).MatchSnapshot();
    }
}
