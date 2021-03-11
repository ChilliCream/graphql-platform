namespace StrawberryShake
{
    /// <summary>
    /// The entity mapper maps the data from an entity to a new read-only model
    /// that is used by the result data object.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TModel">The model type.</typeparam>
    public interface IEntityMapper<in TEntity, out TModel>
        where TEntity : class
        where TModel : class
    {
        /// <summary>
        /// Maps the data from the entity to a new read-only model
        /// that is used by the result data object.
        /// </summary>
        /// <param name="entity">
        /// The entity from which we want to create the model.
        /// </param>
        /// <param name="snapshot">
        /// An optional store snapshot that shall be used instead of the newest snapshot.
        /// </param>
        /// <returns>
        /// Returns a new read-only instance of the model that was created by using the entity.
        /// </returns>
        TModel Map(TEntity entity, IEntityStoreSnapshot? snapshot = null);
    }
}
