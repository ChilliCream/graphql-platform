using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Remove
{
    public class GetFooQuery
    {
        private readonly IOperationExecutor<GetFooResult> _operationExecutor;
        private readonly IOperationStore _operationStore;

        public GetFooQuery(
            IOperationExecutor<GetFooResult> operationExecutor,
            IOperationStore operationStore)
        {
            _operationExecutor = operationExecutor;
            _operationStore = operationStore;
        }

        public async Task<IOperationResult<GetFooResult>> ExecuteAsync(
            string a,
            string? b,
            string? c,
            CancellationToken cancellationToken = default)
        {
            if (a is null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            OperationRequest request = CreateRequest(a, b, c);

            return await _operationExecutor
                .ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        public IOperationObservable<GetFooResult> Watch(
            string a,
            string? b,
            string? c)
        {
            if (a is null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            OperationRequest request = CreateRequest(a, b, c);

            return _operationExecutor.Watch(request);
        }

        private OperationRequest CreateRequest(
            string a,
            string? b,
            string? c) =>
            new(
                "GetFoo",
                GetFooQueryDocument.Instance,
                new Dictionary<string, object?>
                {
                   { "a", a },
                   { "b", b },
                   { "c", c }
                });
    }
}
