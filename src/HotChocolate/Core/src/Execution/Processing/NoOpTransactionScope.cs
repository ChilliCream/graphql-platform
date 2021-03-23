namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// This transaction scope represents a non transactional mutation transaction scope.
    /// </summary>
    internal sealed class NoOpTransactionScope : ITransactionScope
    {
        public void Complete()
        {
        }

        public void Dispose()
        {
        }
    }
}
