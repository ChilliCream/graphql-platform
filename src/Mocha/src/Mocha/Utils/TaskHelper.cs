namespace Mocha;

internal static class TaskHelper
{
    /// <summary>
    /// Waits for all <paramref name="tasks"/> to complete. A single failure is rethrown unchanged, and
    /// several failures are rethrown as one <see cref="AggregateException"/>. A cancelled task is only
    /// rethrown when no task failed.
    /// </summary>
    public static async Task WhenAllAsync(IEnumerable<Task> tasks)
    {
        var whenAll = Task.WhenAll(tasks);

        try
        {
            await whenAll;
        }
        catch when (whenAll.Exception is { InnerExceptions.Count: > 1 } exception)
        {
            throw exception;
        }
    }
}
