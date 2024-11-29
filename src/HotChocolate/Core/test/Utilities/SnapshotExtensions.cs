using CookieCrumble.Formatters;
using HotChocolate.Execution;

namespace HotChocolate.Tests;

public static class SnapshotExtensions
{
    public static async Task<IExecutionResult> MatchSnapshotAsync(
        this Task<IExecutionResult> task,
        object? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
    {
        var result = await task;
        result.MatchSnapshot(postFix, extension, formatter);
        return result;
    }

    public static async Task<string> MatchSnapshotAsync(
        this Task<string> task)
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
