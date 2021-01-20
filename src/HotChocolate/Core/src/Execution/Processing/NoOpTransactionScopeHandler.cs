namespace HotChocolate.Execution.Pipeline
{
    /// <summary>
    /// This transaction scope handler represents creates
    /// a non transactional mutation transaction scope.
    /// </summary>
    internal sealed class NoOpTransactionScopeHandler : ITransactionScopeHandler
    {
        private readonly NoOpTransactionScope _noOpTransaction = new();

        public ITransactionScope Create(IRequestContext context) => _noOpTransaction;
    }
}
