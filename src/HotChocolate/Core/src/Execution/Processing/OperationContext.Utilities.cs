using HotChocolate.Execution.Instrumentation;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
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

    /// <summary>
    /// Gets the type converter service.
    /// </summary>
    /// <value></value>
    public ITypeConverter Converter { get; }
}
