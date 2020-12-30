using System;
using System.Collections.Generic;

namespace StrawberryShake.Impl
{
    /// <summary>
    /// A non generic marker interface for the operation store implementation.
    /// </summary>
    internal interface IStoredOperation : IDisposable
    {
        /// <summary>
        /// The entities that were used to create this result.
        /// </summary>
        IReadOnlyCollection<EntityId> EntityIds { get; }

        /// <summary>
        /// This will trigger the stored operation to rebuild the result from the entity store.
        /// </summary>
        void UpdateResult();
    }
}
