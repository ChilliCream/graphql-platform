using System;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal interface IOperationContext : IHasContextData
    {
        /// <summary>
        /// Gets the schema on which the query is being executed.
        /// </summary>
        ISchema Schema { get; }

        /// <summary>
        /// Gets the request scope services
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets the error handler which adds additional context
        /// data to errors and exceptions.
        /// </summary>
        IErrorHandler ErrorHandler { get; }

        /// <summary>
        /// Gets the diagnostic events.
        /// </summary>
        IDiagnosticEvents DiagnosticEvents { get; }

        /// <summary>
        /// Gets the operation that is being executed.
        /// </summary>
        /// <value></value>
        IPreparedOperation Operation { get; }

        /// <summary>
        /// Gets the value representing the instance of the
        /// <see cref="Operation.RootType" />
        /// </summary>
        object? RootValue { get; }

        /// <summary>
        /// Gets the coerced variable values for the current operation.
        /// </summary>
        IVariableValueCollection Variables { get; }

        /// <summary>
        /// Gets a cancellation token is used to signal
        /// if the request has be aborted.
        /// </summary>
        CancellationToken RequestAborted { get; }

        /// <summary>
        /// Gets the activator helper class.
        /// </summary>
        IActivator Activator { get; }

        /// <summary>
        /// Gets the type converter service.
        /// </summary>
        /// <value></value>
        ITypeConverter Converter { get; }

        IResultHelper Result { get; }

        IExecutionContext Execution { get; }

        // TODO : documentation -> remember this are the raw collected fields without visibility
        ISelectionSet CollectFields(
            SelectionSetNode selectionSet, 
            ObjectType typeContext);
    }
}