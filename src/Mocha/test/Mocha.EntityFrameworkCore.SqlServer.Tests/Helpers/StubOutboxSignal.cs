using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore.SqlServer.Tests.Helpers;

internal sealed class StubOutboxSignal : IOutboxSignal
{
    public int SetCallCount { get; private set; }
    public bool WasSet => SetCallCount > 0;

    public void Set() => SetCallCount++;

    public Task WaitAsync(CancellationToken cancellationToken) => Task.Delay(Timeout.Infinite, cancellationToken);
}
