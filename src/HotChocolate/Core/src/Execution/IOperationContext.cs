using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal interface IOperationContext
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
        /// Gets the query result.
        /// </summary>
        /// <value></value>
        IQueryResultBuilder Result { get; }

        /// <summary>
        /// The context data dictionary can be used by middlewares and
        /// resolvers to store and retrieve data during execution.
        /// </summary>
        IDictionary<string, object?> ContextData { get; }

        /// <summary>
        /// Gets a cancellation token is used to signal
        /// if the request has be aborted.
        /// </summary>
        CancellationToken RequestAborted { get; }

        IReadOnlyList<IPreparedSelection> CollectFields(
            SelectionSetNode selectionSet,
            ObjectType objectType);

        /// <summary>
        /// Gets the activator helper class.
        /// </summary>
        IActivator Activator { get; }

        /// <summary>
        /// Gets the diagnostics writer for query execution.
        /// </summary>
        // QueryExecutionDiagnostics Diagnostics { get; }

        /// <summary>
        /// Gets the type conversion service.
        /// </summary>
        /// <value></value>
        ITypeConversion Converter { get; }

        /// <summary>
        /// Adds an error thread-safe to the result object.
        /// </summary>
        /// <param name="error">The error that shall be added.</param>
        void AddError(IError error);

        /// <summary>
        /// Rewrites the value literals and replaces the variables.
        /// </summary>
        /// <param name="value">
        /// A literal containing variables.
        /// </param>
        /// <param name="type">
        /// The type of which the literal is.
        /// </param>
        /// <returns>
        /// Returns a rewritten literal.
        /// </returns>
        IValueNode ReplaceVariables(IValueNode value, IType type);
    }
}
