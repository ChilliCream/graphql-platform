using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution;

/// <summary>
/// Encapsulates all GraphQL-specific information about an individual GraphQL request.
/// </summary>
public interface IRequestContext : IHasContextData
{
    /// <summary>
    /// Gets the GraphQL schema on which the query is executed.
    /// </summary>
    ISchema Schema { get; }

    /// <summary>
    /// Gets the request executor version.
    /// </summary>
    ulong ExecutorVersion { get; }
    
    /// <summary>
    /// Gets the index of the request that corresponds to this context.
    /// </summary>
    int? RequestIndex { get; }

    /// <summary>
    /// Gets or sets the scoped request services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the error handler which adds additional context
    /// data to errors and exceptions.
    /// </summary>
    IErrorHandler ErrorHandler { get; }

    /// <summary>
    /// Gets the diagnostic events logger.
    /// </summary>
    IExecutionDiagnosticEvents DiagnosticEvents { get; }

    /// <summary>
    /// Gets or sets the initial query request.
    /// </summary>
    IOperationRequest Request { get; }

    /// <summary>
    /// Notifies when the connection underlying this request is aborted
    /// and thus request operations should be cancelled.
    /// </summary>
    CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Gets or sets a unique identifier for a query document.
    /// </summary>
    OperationDocumentId? DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the document hash.
    /// </summary>
    string? DocumentHash { get; set; }

    /// <summary>
    /// Gets or sets the parsed query document.
    /// </summary>
    DocumentNode? Document { get; set; }

    /// <summary>
    /// Defines that the document was retrieved from cache.
    /// </summary>
    bool IsCachedDocument { get; set; }

    /// <summary>
    /// Defines that the document was retrieved from a query storage.
    /// </summary>
    bool IsPersistedDocument { get; set; }

    /// <summary>
    /// Gets or sets the document validation result.
    /// </summary>
    DocumentValidatorResult? ValidationResult { get; set; }

    /// <summary>
    /// <c>true</c> if the document is valid.
    /// <c>false</c> if the document was either not validated or of the document is not valid.
    /// </summary>
    /// <value></value>
    bool IsValidDocument { get; }

    /// <summary>
    /// Gets a unique identifier for a prepared operation.
    /// </summary>
    string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the prepared operation.
    /// </summary>
    IOperation? Operation { get; set; }

    /// <summary>
    /// Gets or sets the coerced variable values.
    /// </summary>
    IReadOnlyList<IVariableValueCollection>? Variables { get; set; }

    /// <summary>
    /// Gets or sets the execution result.
    /// </summary>
    IExecutionResult? Result { get; set; }

    /// <summary>
    /// Gets or sets an unexpected execution exception.
    /// </summary>
    Exception? Exception { get; set; }

    /// <summary>
    /// Clones the request context.
    /// </summary>
    IRequestContext Clone();
}
