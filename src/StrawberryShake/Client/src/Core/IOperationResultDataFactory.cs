namespace StrawberryShake
{
    /// <summary>
    /// This factory Creates the data object of a operation
    /// result by using the data info which provides
    /// all the entity ids used by the result.
    /// </summary>
    /// <typeparam name="TResultData">
    /// The runtime type of the result data object.
    /// </typeparam>
    public interface IOperationResultDataFactory<out TResultData> where TResultData : class
    {
        /// <summary>
        /// Creates the data object of a operation result by using the data info which provides
        /// all the entity ids used by the result.
        /// </summary>
        /// <param name="dataInfo">
        /// The data info that provides all the entity ids used by the result.
        /// </param>
        /// <param name="snapshot">
        /// An optional store snapshot that shall be used instead of the newest snapshot.
        /// </param>
        /// <returns>
        /// Returns the constructed result model.
        /// </returns>
        TResultData Create(
            IOperationResultDataInfo dataInfo,
            IEntityStoreSnapshot? snapshot = null);
    }
}
