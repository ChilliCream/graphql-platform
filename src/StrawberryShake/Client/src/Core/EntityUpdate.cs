using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public sealed class EntityUpdate
    {
        public EntityUpdate(ISet<EntityId> updatedEntityIds, ulong version)
        {
            UpdatedEntityIds = updatedEntityIds ??
                throw new ArgumentNullException(nameof(updatedEntityIds));
            Version = version;
        }

        /// <summary>
        /// Gets the ids of the updated entities.
        /// </summary>
        public ISet<EntityId> UpdatedEntityIds { get; }

        /// <summary>
        /// Gets the store version.
        /// </summary>
        public ulong Version { get; }
    }
}
