using System;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// The internal operation execution context.
    /// </summary>
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

        /// <summary>
        /// The result helper which provides utilities to build up the result.
        /// </summary>
        IResultHelper Result { get; }

        /// <summary>
        /// The execution context proved the processing state.
        /// </summary>
        IExecutionContext Execution { get; }

        /// <summary>
        /// Get the fields for the specified selection set according to the execution plan.
        /// The selection set will show all possibilities and needs to be pre-processed.
        /// </summary>
        /// <param name="selectionSet">
        /// The selection set syntax for which we want to get the compiled selection set.
        /// </param>
        /// <param name="typeContext">
        /// The type context.
        /// </param>
        /// <returns></returns>
        ISelectionSet CollectFields(
            SelectionSetNode selectionSet,
            ObjectType typeContext);

        /// <summary>
        /// Register cleanup tasks that will be executed after resolver execution is finished.
        /// </summary>
        /// <param name="action">
        /// Cleanup action.
        /// </param>
        void RegisterForCleanup(Action action);

        /// <summary>
        /// Get the query root instance.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the query root.
        /// </typeparam>
        /// <returns>
        /// Returns the query root instance.
        /// </returns>
        T GetQueryRoot<T>();
    }
}
