using System;

namespace HotChocolate.Runtime
{
    public sealed class CacheEntryEventArgs<TValue>
        : EventArgs
    {
        internal CacheEntryEventArgs(string key, TValue value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value;
        }

        public string Key { get; }
        public TValue Value { get; }
    }
}
