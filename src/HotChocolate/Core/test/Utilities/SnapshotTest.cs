namespace HotChocolate.Tests;

public sealed class SnapshotTest
{
    private readonly Snapshot _snapshot;
    private readonly Func<Snapshot, CancellationToken, Task> _action;
    private int _allowedRetries = 3;
    private int _timeout = 30_000;

    private SnapshotTest(
        Func<Snapshot, CancellationToken, Task> action,
        string? postFix = null,
        string? extension = null)
    {
        _snapshot = new Snapshot(postFix, extension);
        _action = action;
    }

    public static SnapshotTest Create(
        Func<Snapshot, CancellationToken, Task> action,
        string? postFix = null,
        string? extension = null)
        => new(action, postFix: postFix, extension: extension);

    public SnapshotTest SetRetries(int retries)
    {
        _allowedRetries = retries;
        return this;
    }

    public SnapshotTest SetTimeout(int milliseconds)
    {
        _timeout = milliseconds;
        return this;
    }

    public async Task RunAsync()
        => await TryRunAsync(_snapshot, _action, _allowedRetries, _timeout, match: false).ConfigureAwait(false);

    public async Task MatchAsync()
        => await TryRunAsync(_snapshot, _action, _allowedRetries, _timeout).ConfigureAwait(false);

    private static async Task TryRunAsync(
        Snapshot snapshot,
        Func<Snapshot, CancellationToken, Task> action,
        int allowedRetries = 3,
        int timeout = 30_000,
        bool match = true)
    {
        // we will try four times ....
        var attempt = 0;
        var wait = 250;

        while (true)
        {
            attempt++;

            var success = await ExecuteAsync(attempt).ConfigureAwait(false);

            if (success)
            {
                break;
            }

            await Task.Delay(wait).ConfigureAwait(false);
            wait *= 2;
        }
        return;

        // ReSharper disable once VariableHidesOuterVariable
        async Task<bool> ExecuteAsync(int attempt)
        {
            using var cts = new CancellationTokenSource(timeout);
            var ct = cts.Token;

            if (attempt < allowedRetries)
            {
                try
                {
                    snapshot.Clear();
                    await action(snapshot, ct).ConfigureAwait(false);

                    if (match)
                    {
                        snapshot.MatchMarkdown();
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            snapshot.Clear();
            await action(snapshot, ct).ConfigureAwait(false);

            if (match)
            {
                snapshot.MatchMarkdown();
            }

            return true;
        }
    }
}
