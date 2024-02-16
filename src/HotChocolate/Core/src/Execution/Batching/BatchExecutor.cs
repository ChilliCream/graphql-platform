using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Batching;

internal partial class BatchExecutor
{
    private readonly IErrorHandler _errorHandler;
    private readonly ITypeConverter _typeConverter;
    private readonly InputFormatter _inputFormatter;
    private readonly IReadStoredQueries _readStoredQueries;
    public BatchExecutor(
        IErrorHandler errorHandler,
        ITypeConverter typeConverter,
        InputFormatter inputFormatter,
        IReadStoredQueries readStoredQueries
    )
    {
        _errorHandler = errorHandler ??
            throw new ArgumentNullException(nameof(errorHandler));
        _typeConverter = typeConverter ??
            throw new ArgumentNullException(nameof(typeConverter));
        _inputFormatter = inputFormatter ??
            throw new ArgumentNullException(nameof(inputFormatter));
        _readStoredQueries = readStoredQueries;

    }

    public IAsyncEnumerable<IQueryResult> ExecuteAsync(
        IRequestExecutor requestExecutor,
        IReadOnlyList<IQueryRequest> requestBatch)
        => new BatchExecutorEnumerable(
            requestBatch,
            requestExecutor,
            _errorHandler,
            _typeConverter,
            _inputFormatter,_readStoredQueries);
}
