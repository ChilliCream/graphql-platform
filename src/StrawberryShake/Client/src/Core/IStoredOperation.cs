using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    /// <summary>
    /// A non generic marker interface for the operation store implementation.
    /// </summary>
    internal interface IStoredOperation : IDisposable
    {
        /// <summary>
        /// Gets the entities that were used to create this result.
        /// </summary>
        IReadOnlyCollection<EntityId> EntityIds { get; }

        /// <summary>
        /// Gets the current entity store version of this operation.
        /// </summary>
        ulong Version { get; }

        /// <summary>
        /// This will trigger the stored operation to rebuild the result from the entity store.
        /// </summary>
        void UpdateResult(ulong version);
    }
}
