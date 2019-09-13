using System;
using System.Threading.Tasks;

namespace GreenDonut
{
    internal static class SynchronizationExtensions
    {
        public static void Lock(
            this object sync,
            Func<bool> predicate,
            Action execute)
        {
            if (predicate())
            {
                lock (sync)
                {
                    if (predicate())
                    {
                        execute();
                    }
                }
            }
        }

        public static Task LockAsync(
            this object sync,
            Func<bool> predicate,
            Func<Task> execute)
        {
            if (predicate())
            {
                lock (sync)
                {
                    if (predicate())
                    {
                        return execute();
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
