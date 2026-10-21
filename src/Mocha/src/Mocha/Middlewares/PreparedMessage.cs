using Microsoft.Extensions.ObjectPool;

namespace Mocha.Middlewares;

/// <summary>
/// Owns a prepared outgoing message until it is disposed.
/// </summary>
internal readonly struct PreparedMessage : IDisposable
{
    private readonly DispatchContext? _context;
    private readonly ObjectPool<DispatchContext>? _pool;
    private readonly long _lease;

    internal PreparedMessage(DispatchContext context, ObjectPool<DispatchContext> pool)
    {
        _context = context;
        _pool = pool;
        _lease = context.AcquirePreparedMessageLease();
    }

    /// <summary>
    /// Gets the prepared envelope, which remains valid until this message is disposed.
    /// </summary>
    public MessageEnvelope Envelope
    {
        get
        {
            if (_context is null || !_context.IsPreparedMessageLeaseCurrent(_lease))
            {
                throw ThrowHelper.PreparedMessageDisposed();
            }

            return _context.Envelope!;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_context?.ReleasePreparedMessageLease(_lease) == true)
        {
            _pool!.Return(_context);
        }
    }
}
