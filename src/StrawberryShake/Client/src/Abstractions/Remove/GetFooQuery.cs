using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http;

namespace StrawberryShake.Remove
{
    public class GetFooQuery
    {
        private readonly IOperationExecutor<GetFooResult> _operationExecutor;

        public GetFooQuery(IOperationExecutor<GetFooResult> operationExecutor)
        {
            _operationExecutor = operationExecutor ??
                throw new ArgumentNullException(nameof(operationExecutor));
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
            string? c,
            ExecutionStrategy? strategy = null)
        {
            if (a is null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            OperationRequest request = CreateRequest(a, b, c);

            return _operationExecutor.Watch(request, strategy);
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
