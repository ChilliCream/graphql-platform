using System.Diagnostics;
using System.Threading;

namespace HotChocolate.Execution
{
    internal sealed class ObjectPool<T>
       where T : class, IShared, new()
    {
        private readonly T[] _objects;

        // do not make this readonly; it's a mutable struct
        private SpinLock _lock;
        private int _index;

        internal ObjectPool(int numberOfBuffers)
        {
            // only enable thread tracking if debugger is attached;
            // it adds non-trivial overheads to Enter/Exit
            _lock = new SpinLock(Debugger.IsAttached);
            _objects = new T[numberOfBuffers];
        }

        internal T Rent()
        {
            T[] objects = _objects;
            T obj = null;

            // While holding the lock, grab whatever is at the next available
            // index and update the index.  We do as little work as possible
            // while holding the spin lock to minimize contention with
            // other threads. The try/finally is necessary to properly handle
            // thread aborts on platforms which have them.
            bool lockTaken = false;
            bool allocateBuffer = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index < objects.Length)
                {
                    obj = objects[_index];
                    objects[_index++] = null;
                    allocateBuffer = obj == null;
                }
            }
            finally
            {
                if (lockTaken) { _lock.Exit(false); }
            }

            // While we were holding the lock, we grabbed whatever was at
            // the next available index, if there was one. If we tried and
            // if we got back null, that means we hadn't yet allocated
            // for that slot, in which case we should do so now.
            if (allocateBuffer)
            {
                obj = new T();
            }

            return obj;
        }

        internal void Return(T rendetObject)
        {
            // While holding the spin lock, if there's room available
            // in the bucket, put the buffer into the next available slot.
            // Otherwise, we just drop it. The try/finally is necessary to
            // properly handle thread aborts on platforms which have them.
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);

                if (_index != 0)
                {
                    rendetObject.Clean();
                    _objects[--_index] = rendetObject;
                }
            }
            finally
            {
                if (lockTaken) { _lock.Exit(false); }
            }
        }
    }
}
