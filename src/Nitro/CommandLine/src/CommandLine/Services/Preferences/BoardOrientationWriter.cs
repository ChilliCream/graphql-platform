using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Services.Preferences;

/// <summary>
/// Saves board orientation changes through an <see cref="IBoardPreferencesStore"/> in the
/// background, one write at a time. A change made while a write is running replaces any
/// change still waiting, so the last orientation handed to <see cref="Enqueue"/> is the one
/// persisted.
/// </summary>
internal sealed class BoardOrientationWriter(IBoardPreferencesStore store)
{
    private readonly object _gate = new();
    private BoardOrientation? _pending;
    private Task _worker = Task.CompletedTask;
    private bool _running;

    /// <summary>
    /// Schedules <paramref name="orientation"/> to be saved without blocking the caller.
    /// A write the store reports as failed is dropped.
    /// </summary>
    public void Enqueue(BoardOrientation orientation)
    {
        lock (_gate)
        {
            _pending = orientation;

            if (_running)
            {
                return;
            }

            _running = true;
            _worker = Task.Run(WriteAsync);
        }
    }

    /// <summary>
    /// Waits up to <paramref name="timeout"/> for every enqueued orientation to be saved.
    /// Returns <see langword="false"/> when writes are still pending after the timeout.
    /// </summary>
    public async Task<bool> DrainAsync(TimeSpan timeout)
    {
        Task worker;

        lock (_gate)
        {
            worker = _worker;
        }

        try
        {
            await worker.WaitAsync(timeout);

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private async Task WriteAsync()
    {
        try
        {
            while (true)
            {
                BoardOrientation next;

                lock (_gate)
                {
                    if (_pending is not { } pending)
                    {
                        _running = false;
                        return;
                    }

                    next = pending;
                    _pending = null;
                }

                // The store reports an unwritable file by returning false, which keeps the in-memory choice.
                await store.WriteOrientationAsync(next, CancellationToken.None);
            }
        }
        catch
        {
            lock (_gate)
            {
                _running = false;
            }

            throw;
        }
    }
}
