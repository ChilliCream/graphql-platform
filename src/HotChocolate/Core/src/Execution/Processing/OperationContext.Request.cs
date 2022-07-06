using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
    /// <summary>
    /// Gets the schema on which the query is being executed.
    /// </summary>
    public ISchema Schema
    {
        get
        {
            AssertInitialized();
            return _schema;
        }
    }

    /// <summary>
    /// Gets the error handler which adds additional context
    /// data to errors and exceptions.
    /// </summary>
    public IErrorHandler ErrorHandler
    {
        get
        {
            AssertInitialized();
            return _errorHandler;
        }
    }

    /// <summary>
    /// Gets the activator helper class.
    /// </summary>
    public IActivator Activator
    {
        get
        {
            AssertInitialized();
            return _activator;
        }
    }

    /// <summary>
    /// Gets the diagnostic events.
    /// </summary>
    public IExecutionDiagnosticEvents DiagnosticEvents
    {
        get
        {
            AssertInitialized();
            return _diagnosticEvents;
        }
    }

    public IDictionary<string, object?> ContextData
    {
        get
        {
            AssertInitialized();
            return _contextData;
        }
    }

    /// <summary>
    /// Gets a cancellation token is used to signal
    /// if the request has be aborted.
    /// </summary>
    public CancellationToken RequestAborted
    {
        get
        {
            AssertInitialized();
            return _requestAborted;
        }
    }
}
