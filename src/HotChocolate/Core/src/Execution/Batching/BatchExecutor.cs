using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Batching
{
    internal partial class BatchExecutor
    {
        private readonly IRequestExecutor _requestExecutor;
        private readonly IErrorHandler _errorHandler;
        private readonly ITypeConversion _typeConversion;

        public BatchExecutor(
            IRequestExecutor requestExecutor,
            IErrorHandler errorHandler,
            ITypeConversion typeConversion)
        {
            _requestExecutor = requestExecutor ??
                throw new ArgumentNullException(nameof(requestExecutor));
            _errorHandler = errorHandler ??
                throw new ArgumentNullException(nameof(errorHandler));
            _typeConversion = typeConversion ??
                throw new ArgumentNullException(nameof(typeConversion));
        }

        public IAsyncEnumerable<IQueryResult> ExecuteAsync(
            IEnumerable<IReadOnlyQueryRequest> requestBatch,
            CancellationToken cancellationToken = default)
        {
            return new BatchExecutorEnumerable(
                requestBatch, _requestExecutor, _errorHandler, _typeConversion);
        }
    }
}
