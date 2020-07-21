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
        private readonly ITypeConverter _typeConverter;

        public BatchExecutor(
            IRequestExecutor requestExecutor,
            IErrorHandler errorHandler,
            ITypeConverter typeConverter)
        {
            _requestExecutor = requestExecutor ??
                throw new ArgumentNullException(nameof(requestExecutor));
            _errorHandler = errorHandler ??
                throw new ArgumentNullException(nameof(errorHandler));
            _typeConverter = typeConverter ??
                throw new ArgumentNullException(nameof(typeConverter));
        }

        public IAsyncEnumerable<IQueryResult> ExecuteAsync(
            IEnumerable<IReadOnlyQueryRequest> requestBatch,
            CancellationToken cancellationToken = default)
        {
            return new BatchExecutorEnumerable(
                requestBatch, _requestExecutor, _errorHandler, _typeConverter);
        }
    }
}
