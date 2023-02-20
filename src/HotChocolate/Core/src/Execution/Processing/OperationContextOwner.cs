using System;
using Microsoft.Extensions.ObjectPool;
using static System.Threading.Interlocked;

namespace HotChocolate.Execution.Processing;

internal sealed class OperationContextOwner : IDisposable
{
    private readonly ObjectPool<OperationContext> _pool;
    private readonly OperationContext _context;
    private int _disposed;

    public OperationContextOwner(ObjectPool<OperationContext> operationContextPool)
    {
        _pool = operationContextPool;
        _context = operationContextPool.Get();
    }

    public OperationContext OperationContext
    {
        get
        {
            if (_disposed == 1)
            {
                throw new ObjectDisposedException(nameof(OperationContextOwner));
            }

            return _context;
        }
    }

    public void Dispose()
    {
        if (_disposed == 0 && CompareExchange(ref _disposed, 1, 0) == 0)
        {
            _pool.Return(_context);
        }
    }
}
