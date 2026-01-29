using Microsoft.Extensions.ObjectPool;
using static System.Threading.Interlocked;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// <para>
/// The operation context owner abstracts the interaction of resolving of
/// an <see cref="OperationContext"/> instance from its pool and returning to
/// the pool through the implementation of <see cref="IDisposable"/>.
/// </para>
/// <para>
/// In some cases its desirable to not call dispose and abandon a pooled
/// <see cref="OperationContext"/>.
/// </para>
/// </summary>
internal sealed class OperationContextOwner : IDisposable
{
    private readonly ObjectPool<OperationContext> _pool;
    private readonly OperationContext _context;
    private int _disposed;

    public OperationContextOwner(ObjectPool<OperationContext> operationContextPool)
    {
        ArgumentNullException.ThrowIfNull(operationContextPool);

        _pool = operationContextPool;
        _context = operationContextPool.Get();
    }

    /// <summary>
    /// Gets the pooled <see cref="OperationContext"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The operation context was already return to the pool.
    /// </exception>
    public OperationContext OperationContext
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);

            return _context;
        }
    }

    /// <summary>
    /// Returns the <see cref="OperationContext"/> back to its pool.
    /// </summary>
    public void Dispose()
    {
        if (_disposed == 0 && CompareExchange(ref _disposed, 1, 0) == 0)
        {
            _pool.Return(_context);
        }
    }
}
