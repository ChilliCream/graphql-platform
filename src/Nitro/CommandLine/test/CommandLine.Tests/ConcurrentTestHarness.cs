namespace ChilliCream.Nitro.CommandLine.Tests;

internal static class ConcurrentTestHarness
{
    internal static async Task<TResult[]> RunAsync<TResult>(int callerCount, Func<int, Task<TResult>> action)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(callerCount, 1);
        ArgumentNullException.ThrowIfNull(action);

        var allReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var remaining = callerCount;

        var callers = Enumerable.Range(1, callerCount)
            .Select(caller => Task.Run(async () =>
            {
                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    allReady.TrySetResult();
                }

                await start.Task;
                return await action(caller);
            }))
            .ToArray();

        await allReady.Task;
        start.TrySetResult();

        return await Task.WhenAll(callers);
    }
}
