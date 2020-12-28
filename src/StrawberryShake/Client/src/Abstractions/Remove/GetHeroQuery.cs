using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Impl;

namespace StrawberryShake.Remove
{
    public class GetHeroQuery
    {
        private readonly IOperationExecutor<GetHeroResult> _operationExecutor;

        public GetHeroQuery(IOperationExecutor<GetHeroResult> operationExecutor)
        {
            _operationExecutor = operationExecutor ??
                throw new ArgumentNullException(nameof(operationExecutor));
        }

        public async Task<IOperationResult<GetHeroResult>> ExecuteAsync(
            CancellationToken cancellationToken = default)
        {
            OperationRequest request = CreateRequest();
            return await _operationExecutor
                .ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        public IOperationObservable<GetHeroResult> Watch(
            ExecutionStrategy? strategy = null)
        {
            OperationRequest request = CreateRequest();
            return _operationExecutor.Watch(request, strategy);
        }

        private OperationRequest CreateRequest() =>
            new(
                "GetHero",
                GetHeroQueryDocument.Instance);
    }
}
