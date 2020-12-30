using System;

namespace StrawberryShake
{
    public interface IEntityUpdateSession : IDisposable
    {
        /// <summary>
        /// Gets the store version.
        /// </summary>
        public ulong Version { get; }
    }
}
