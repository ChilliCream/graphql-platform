namespace HotChocolate.Execution.Pipeline
{
    /// <summary>
    /// Allows to make mutation execution transactional.
    /// </summary>
    public interface ITransactionScopeHandler
    {
        /// <summary>
        /// Creates a new transaction scope for the current
        /// request represented by the <see cref="IRequestContext"/>.
        /// </summary>
        /// <param name="context">
        /// The GraphQL request context.
        /// </param>
        /// <returns>
        /// Returns a new <see cref="ITransactionScope"/>.
        /// </returns>
        ITransactionScope Create(IRequestContext context);
    }
}
