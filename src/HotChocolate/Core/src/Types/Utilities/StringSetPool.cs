using System.Diagnostics;

namespace HotChocolate.Utilities;

internal sealed class StringSetPool(int size = 64)
{
    private readonly Bucket _bucket = new(size);

    public HashSet<string> Rent()
        => _bucket.Rent() ?? [];

    public void Return(HashSet<string> set)
    {
        Debug.Assert(set != null);
        set.Clear();
        _bucket.Return(set);
    }

    public static StringSetPool Shared { get; } = new();

    private sealed class Bucket
    {
        private readonly HashSet<string>?[] _sets;
        private SpinLock _lock;
        private int _index;

        internal Bucket(int size)
        {
            _sets = new HashSet<string>[size];
            _lock = new SpinLock(false);
            _index = 0;
        }

        internal HashSet<string>? Rent()
        {
            HashSet<string>? set = null;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index < _sets.Length)
                {
                    set = _sets[_index];
                    _sets[_index++] = null;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            return set;
        }

        internal void Return(HashSet<string> set)
        {
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index > 0)
                {
                    _sets[--_index] = set;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
        }
    }
}
