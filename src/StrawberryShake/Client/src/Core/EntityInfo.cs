namespace StrawberryShake
{
    /// <summary>
    /// The entity info allows access to the entity and the id of the entity.
    /// </summary>
    public readonly struct EntityInfo
    {
        /// <summary>
        /// Creates a new instance of <see cref="EntityInfo"/>.
        /// </summary>
        /// <param name="id">
        /// The Entity object.
        /// </param>
        /// <param name="entity">
        /// The entity id.
        /// </param>
        public EntityInfo(EntityId id, object entity)
        {
            Id = id;
            Entity = entity;
        }

        /// <summary>
        /// Gets the entity id.
        /// </summary>
        public EntityId Id { get; }

        /// <summary>
        /// Gets the entity object.
        /// </summary>
        public object Entity { get; }
    }
}
