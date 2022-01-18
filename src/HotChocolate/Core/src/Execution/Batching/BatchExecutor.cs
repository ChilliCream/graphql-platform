using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Batching;

internal partial class BatchExecutor
{
    private readonly IErrorHandler _errorHandler;
    private readonly ITypeConverter _typeConverter;
    private readonly InputFormatter _inputFormatter;
    private readonly IRequestBatchOptions _batchOptions;
    private readonly IExecutionDiagnosticEvents _executionDiagnosticEvents;

    public BatchExecutor(
        IErrorHandler errorHandler,
        ITypeConverter typeConverter,
        InputFormatter inputFormatter,
        IExecutionDiagnosticEvents executionDiagnosticEvents,
        IRequestBatchOptions batchOptions)
    {
        _errorHandler = errorHandler ??
            throw new ArgumentNullException(nameof(errorHandler));
        _typeConverter = typeConverter ??
            throw new ArgumentNullException(nameof(typeConverter));
        _inputFormatter = inputFormatter ??
            throw new ArgumentNullException(nameof(inputFormatter));
        _batchOptions = batchOptions ??
            throw new ArgumentNullException(nameof(batchOptions));
        _executionDiagnosticEvents = executionDiagnosticEvents ??
            throw new ArgumentNullException(nameof(executionDiagnosticEvents));
    }

    public IAsyncEnumerable<IQueryResult> ExecuteAsync(
        IRequestExecutor requestExecutor,
        IEnumerable<IQueryRequest> requestBatch,
        bool allowParallelExecution = false)
    {
        return new BatchExecutorEnumerable(
            requestBatch,
            requestExecutor,
            _errorHandler,
            _typeConverter,
            _inputFormatter,
            _executionDiagnosticEvents,
            allowParallelExecution ? _batchOptions.MaxConcurrentBatchQueries : 1);
    }
}
