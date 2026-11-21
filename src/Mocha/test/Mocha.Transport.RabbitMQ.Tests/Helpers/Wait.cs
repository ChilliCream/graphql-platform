using System.Collections;
using System.Runtime.CompilerServices;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

internal static class TestTimeouts
{
    /// <summary>
    /// The ceiling for anything a test expects to happen. It is never an expected duration.
    /// </summary>
    public static readonly TimeSpan Signal = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The ceiling for one command that runs inside a broker container, which includes a broker restart.
    /// </summary>
    public static readonly TimeSpan Command = TimeSpan.FromSeconds(60);
}

internal static class Wait
{
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan s_localPollInterval = TimeSpan.FromMilliseconds(25);

    /// <summary>
    /// Polls <paramref name="condition"/> until it holds, and throws a <see cref="TimeoutException"/> that names
    /// the condition when it does not hold within <see cref="TestTimeouts.Signal"/>.
    /// </summary>
    public static async Task UntilAsync(
        Func<bool> condition,
        [CallerArgumentExpression(nameof(condition))] string expectation = "")
    {
        var deadline = DateTimeOffset.UtcNow.Add(TestTimeouts.Signal);

        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException($"Expected {expectation} to hold within {Ceiling}.");
            }

            await Task.Delay(s_localPollInterval, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it holds, and throws a <see cref="TimeoutException"/> that names
    /// the condition when it does not hold within <see cref="TestTimeouts.Signal"/>.
    /// </summary>
    public static async Task UntilAsync(
        Func<Task<bool>> condition,
        [CallerArgumentExpression(nameof(condition))] string expectation = "")
    {
        var deadline = DateTimeOffset.UtcNow.Add(TestTimeouts.Signal);

        while (!await condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException($"Expected {expectation} to hold within {Ceiling}.");
            }

            await Task.Delay(s_pollInterval, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Reads a state with <paramref name="observe"/> until <paramref name="condition"/> holds for it, and returns
    /// that state. Throws a <see cref="TimeoutException"/> with the last observed state when the condition does
    /// not hold within <see cref="TestTimeouts.Signal"/>.
    /// </summary>
    public static Task<T> UntilAsync<T>(Func<Task<T>> observe, Func<T, bool> condition, string expectation)
        => UntilAsync(observe, condition, expectation, TestTimeouts.Signal);

    /// <summary>
    /// Reads a state with <paramref name="observe"/> until <paramref name="condition"/> holds for it, and returns
    /// that state. Throws a <see cref="TimeoutException"/> with the last observed state when the condition does
    /// not hold within <paramref name="within"/>, a bound that has a meaning of its own in the test.
    /// </summary>
    public static async Task<T> UntilAsync<T>(
        Func<Task<T>> observe,
        Func<T, bool> condition,
        string expectation,
        TimeSpan within)
    {
        var deadline = DateTimeOffset.UtcNow.Add(within);

        while (true)
        {
            var state = await observe();
            if (condition(state))
            {
                return state;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Expected {expectation} within {within.TotalSeconds:0} s, but last observed {Describe(state)}.");
            }

            await Task.Delay(s_pollInterval, TestContext.Current.CancellationToken);
        }
    }

    private static string Ceiling => $"{TestTimeouts.Signal.TotalSeconds:0} s";

    private static string Describe(object? state)
        => state switch
        {
            null => "nothing",
            string text => text,
            IEnumerable items => $"[{string.Join(", ", items.Cast<object?>().Select(Describe))}]",
            _ => state.ToString() ?? "nothing"
        };
}

internal static class Condition
{
    /// <summary>
    /// Polls <paramref name="condition"/> until it holds or <paramref name="window"/> elapses, and returns
    /// whether it held. For something that must happen, use <see cref="Wait"/> instead.
    /// </summary>
    public static async Task<bool> WaitAsync(Func<bool> condition, TimeSpan window)
    {
        var deadline = DateTimeOffset.UtcNow.Add(window);

        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it holds or <paramref name="window"/> elapses, and returns
    /// whether it held. For something that must happen, use <see cref="Wait"/> instead.
    /// </summary>
    public static async Task<bool> WaitAsync(Func<Task<bool>> condition, TimeSpan window)
    {
        var deadline = DateTimeOffset.UtcNow.Add(window);

        while (!await condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return true;
    }
}

internal static class ConnectionExtensions
{
    /// <summary>
    /// Returns a task that completes once the client has seen <paramref name="connection"/> shut down.
    /// </summary>
    public static Task WhenShutdownAsync(this IConnection connection)
    {
        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            shutdown.TrySetResult();
            return Task.CompletedTask;
        };

        if (!connection.IsOpen)
        {
            shutdown.TrySetResult();
        }

        return shutdown.Task;
    }
}
