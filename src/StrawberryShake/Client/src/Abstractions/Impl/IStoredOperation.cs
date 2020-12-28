using System;
using System.Collections.Generic;

namespace StrawberryShake.Impl
{
    /// <summary>
    /// A non generic marker interface for the operation store implementation.
    /// </summary>
    internal interface IStoredOperation : IDisposable
    {
        IReadOnlyCollection<EntityId> EntityIds { get; }
    }
}
