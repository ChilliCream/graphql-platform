using System;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Utilities;
using HotChocolate.Validation;

#nullable enable

namespace HotChocolate.Execution
{
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
        /// Gets or sets the scoped request services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets the error handler which adds additional context
        /// data to errors and exceptions.
        /// </summary>
        IErrorHandler ErrorHandler { get; }

        /// <summary>
        /// Gets the type converter service.
        /// </summary>
        ITypeConverter Converter { get; }

        /// <summary>
        /// Gets the activator helper class.
        /// </summary>
        IActivator Activator { get; }

        /// <summary>
        /// Gets the diagnostic events logger.
        /// </summary>
        IDiagnosticEvents DiagnosticEvents { get; }

        /// <summary>
        /// Gets or sets the initial query request.
        /// </summary>
        IQueryRequest Request { get; }

        /// <summary>
        /// Notifies when the connection underlying this request is aborted
        /// and thus request operations should be cancelled.
        /// </summary>
        CancellationToken RequestAborted { get; set; }

        /// <summary>
        /// Gets or sets a unique identifier for a query document.
        /// </summary>
        string? DocumentId { get; set; }

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
        /// Gets a unique identifier for a prepared operation.
        /// </summary>
        string? OperationId { get; set; }

        /// <summary>
        /// Gets or sets the prepared operation.
        /// </summary>
        IPreparedOperation? Operation { get; set; }

        /// <summary>
        /// Gets or sets the coerced variable values.
        /// </summary>
        IVariableValueCollection? Variables { get; set; }

        /// <summary>
        /// Gets or sets the execution result.
        /// </summary>
        IExecutionResult? Result { get; set; }

        /// <summary>
        /// Gets or sets an unexpected execution exception.
        /// </summary>
        Exception? Exception { get; set; }
    }
}
