using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Types;
using static HotChocolate.Fusion.Execution.OperationPlanContextPoolEventSource;

namespace HotChocolate.Fusion.Execution;

internal sealed class OperationPlanContextPool : IDisposable
{
    private readonly Bucket _bucket;
    private readonly INodeIdParser _nodeIdParser;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IErrorHandler _errorHandler;

    public OperationPlanContextPool(
        INodeIdParser nodeIdParser,
        IFusionExecutionDiagnosticEvents diagnosticEvents,
        IErrorHandler errorHandler,
        int[] levels,
        TimeSpan trimInterval)
    {
        Debug.Assert(
            levels.Length > 0,
            "Levels must be a non-empty array.");
        Debug.Assert(
            trimInterval.TotalSeconds > 10,
            "Trim interval should be greater than 10 seconds to avoid excessive trimming.");

        _nodeIdParser = nodeIdParser;
        _diagnosticEvents = diagnosticEvents;
        _errorHandler = errorHandler;
        _bucket = new Bucket(levels, trimInterval);
    }

    public OperationPlanContext Rent()
    {
        var context = _bucket.Rent();

        if (context is null)
        {
            context = new OperationPlanContext(_nodeIdParser, _diagnosticEvents, _errorHandler);
            Log.ContextMiss();
        }
        else
        {
            Log.ContextHit();
        }

        context._pool = this;
        return context;
    }

    public void Return(OperationPlanContext context)
    {
        context.Clean();

        if (!_bucket.Return(context))
        {
            context.Destroy();
            Log.ContextDropped();
        }
    }

    public void Dispose() => _bucket.Dispose();

    private sealed class Bucket : IDisposable
    {
        private readonly OperationPlanContext?[] _stores;
        private readonly int[] _levels;
        private readonly Timer _trimTimer;
        private int _currentLevel;
        private int _inUse;
        private SpinLock _lock;
        private int _index;

        internal Bucket(int[] levels, TimeSpan trimInterval)
        {
            _stores = new OperationPlanContext?[levels[levels.Length - 1]];
            _levels = levels;
            _currentLevel = 0;
            _lock = new SpinLock(Debugger.IsAttached);
            _trimTimer = new Timer(static b => ((Bucket)b!).Trim(), this, trimInterval, trimInterval);
        }

        internal OperationPlanContext? Rent()
        {
            Interlocked.Increment(ref _inUse);

            OperationPlanContext? context = null;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index >= _levels[_currentLevel] && _currentLevel < _levels.Length - 1)
                {
                    _currentLevel++;
                }

                if (_index < _levels[_currentLevel])
                {
                    context = _stores[_index];
                    _stores[_index++] = null;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            return context;
        }

        internal bool Return(OperationPlanContext context)
        {
            Interlocked.Decrement(ref _inUse);

            var lockTaken = false;
            var accepted = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index > 0)
                {
                    _stores[--_index] = context;
                    accepted = true;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            return accepted;
        }

        private void Trim()
        {
            var currentLevel = _currentLevel;

            if (currentLevel == 0)
            {
                return;
            }

            var previousLevel = currentLevel - 1;
            var previousLimit = _levels[previousLevel];

            if (_inUse > previousLimit)
            {
                return;
            }

            var lockTaken = false;

            try
            {
                var currentLimit = _levels[currentLevel];

                _lock.Enter(ref lockTaken);

                for (var i = previousLimit; i < currentLimit; i++)
                {
                    if (_stores[i] is { } context)
                    {
                        context.Destroy();
                        _stores[i] = null;
                    }
                }

                if (_index > previousLimit)
                {
                    _index = previousLimit;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            _currentLevel = previousLevel;
            Log.PoolTrimmed(previousLevel, previousLimit);
        }

        public void Dispose()
        {
            _trimTimer.Dispose();

            for (var i = 0; i < _stores.Length; i++)
            {
                _stores[i]?.Destroy();
                _stores[i] = null;
            }
        }
    }
}
