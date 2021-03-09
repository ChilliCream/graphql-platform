using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public sealed class EntityUpdate
    {
        public EntityUpdate(
            IEntityStoreSnapshot snapshot,
            ISet<EntityId> updatedEntityIds,
            ulong version)
        {
            Snapshot = snapshot ??
                throw new ArgumentNullException(nameof(snapshot));
            UpdatedEntityIds = updatedEntityIds ??
                throw new ArgumentNullException(nameof(updatedEntityIds));
            Version = version;
        }

        /// <summary>
        /// Gets the store snapshot.
        /// </summary>
        public IEntityStoreSnapshot Snapshot { get; }

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
