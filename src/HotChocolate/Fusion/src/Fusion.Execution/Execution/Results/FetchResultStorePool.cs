using System.Diagnostics;
using static HotChocolate.Fusion.Execution.Results.FetchResultStorePoolEventSource;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed class FetchResultStorePool : IDisposable
{
    private const int MaxCollectTargetRetainLength = 256;
    private const int MaxDictionaryRetainCapacity = 256;

    private readonly Bucket _bucket;

    public FetchResultStorePool(int[] levels, TimeSpan trimInterval)
    {
        Debug.Assert(
            levels.Length > 0,
            "Levels must be a non-empty array.");
        Debug.Assert(
            trimInterval.TotalSeconds > 10,
            "Trim interval should be greater than 10 seconds to avoid excessive trimming.");

        _bucket = new Bucket(levels, trimInterval);
    }

    public FetchResultStore Rent()
    {
        var store = _bucket.Rent();

        if (store is null)
        {
            store = new FetchResultStore();
            Log.StoreMiss();
        }
        else
        {
            Log.StoreHit();
        }

        return store;
    }

    public void Return(FetchResultStore store)
    {
        store.Clean(MaxCollectTargetRetainLength, MaxDictionaryRetainCapacity);

        if (!_bucket.Return(store))
        {
            store.Dispose();
            Log.StoreDropped();
        }
    }

    public void Dispose() => _bucket.Dispose();

    private sealed class Bucket : IDisposable
    {
        private readonly FetchResultStore?[] _stores;
        private readonly int[] _levels;
        private readonly Timer _trimTimer;
        private int _currentLevel;
        private int _inUse;
        private SpinLock _lock;
        private int _index;

        internal Bucket(int[] levels, TimeSpan trimInterval)
        {
            _stores = new FetchResultStore?[levels[levels.Length - 1]];
            _levels = levels;
            _currentLevel = 0;
            _lock = new SpinLock(Debugger.IsAttached);
            _trimTimer = new Timer(static b => ((Bucket)b!).Trim(), this, trimInterval, trimInterval);
        }

        internal FetchResultStore? Rent()
        {
            Interlocked.Increment(ref _inUse);

            FetchResultStore? store = null;
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
                    store = _stores[_index];
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

            return store;
        }

        internal bool Return(FetchResultStore store)
        {
            Interlocked.Decrement(ref _inUse);

            var lockTaken = false;
            var accepted = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index > 0)
                {
                    _stores[--_index] = store;
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
                    if (_stores[i] is { } store)
                    {
                        store.Dispose();
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
                _stores[i]?.Dispose();
                _stores[i] = null;
            }
        }
    }
}
