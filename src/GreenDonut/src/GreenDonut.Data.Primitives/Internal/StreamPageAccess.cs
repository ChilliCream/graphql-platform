namespace GreenDonut.Data.Internal;

internal sealed class StreamPageAccess
{
    private readonly SemaphoreSlim _gate = new(initialCount: 1, maxCount: 1);

    public async ValueTask<Lease> EnterAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Lease(_gate);
    }

    internal readonly struct Lease(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
}
