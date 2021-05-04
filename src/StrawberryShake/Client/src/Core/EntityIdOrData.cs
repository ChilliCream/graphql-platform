namespace StrawberryShake
{
    /// <summary>
    /// <see cref="EntityIdOrData"/> represents a union that can be
    /// a <see cref="EntityId"/> or a data object.
    /// </summary>
    public readonly struct EntityIdOrData
    {
        /// <summary>
        /// Creates a new <see cref="EntityIdOrData"/> instance.
        /// </summary>
        /// <param name="entityId">
        /// The entity id.
        /// </param>
        public EntityIdOrData(EntityId entityId)
        {
            EntityId = entityId;
            Data = null;
        }
        /// <summary>
        /// Creates a new <see cref="EntityIdOrData"/> instance.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        public EntityIdOrData(object data)
        {
            EntityId = null;
            Data = data;
        }

        /// <summary>
        /// Gets the entityId value.
        /// </summary>
        public EntityId? EntityId { get; }

        /// <summary>
        /// Defines if this union is an entityId.
        /// </summary>
        public bool IsEntityId => EntityId is not null;

        /// <summary>
        /// Gets the data value.
        /// </summary>
        public object? Data { get; }

        /// <summary>
        /// Defines if this union is data.
        /// </summary>
        public bool IsData => Data is not null;

        /// <summary>
        /// Implicitly calls <c>new EntityIdOrData(entityId)</c>.
        /// </summary>
        /// <param name="entityId">
        /// The <see cref="entityId"/> that shall be converted.
        /// </param>
        public static implicit operator EntityIdOrData(EntityId entityId) => new(entityId);
    }
}
