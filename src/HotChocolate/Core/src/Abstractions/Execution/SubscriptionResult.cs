using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    public sealed class SubscriptionResult
        : ISubscriptionResult
    {
        private readonly IAsyncEnumerable<IQueryResult>? _resultStream;

        public SubscriptionResult(
            IAsyncEnumerable<IQueryResult>? resultStream,
            IReadOnlyList<IError>? errors,
            IReadOnlyDictionary<string, object?>? extension = null,
            IReadOnlyDictionary<string, object?>? contextData = null,
            IAsyncDisposable? subscription = null)
        {
            _enumerator = new SubscriptionResultEnumerator(
                sourceStream,
                contextFactory,
                executeQuery,
                serviceScope,
                cancellationToken);
        }

        public IReadOnlyList<IError>? Errors => throw new NotImplementedException();

        public IReadOnlyDictionary<string, object?>? Extensions => throw new NotImplementedException();

        public IReadOnlyDictionary<string, object?>? ContextData => throw new NotImplementedException();

        public IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_resultStream is { })
            {
                return _resultStream.GetAsyncEnumerator(cancellationToken);
            }
            
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
