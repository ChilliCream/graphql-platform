using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface IExecutionContext
    {
        /// <summary>
        /// Gets the schema on which the query is being executed.
        /// </summary>
        ISchema Schema { get; }

        /// <summary>
        /// Gets the scoped execution services.
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
        IOperation Operation { get; }

        /// <summary>
        /// Gets the coerced variables.
        /// </summary>
        /// <value></value>
        IVariableCollection Variables { get; }

        /// <summary>
        /// Gets the query response.
        /// </summary>
        /// <value></value>
        IQueryResponse Response { get; }

        /// <summary>
        /// The context data dictionary can be used by middlewares and
        /// resolvers to store and retrieve data during execution.
        /// </summary>
        IDictionary<string, object> ContextData { get; }

        /// <summary>
        /// Gets a cancellation token is used to signal
        /// if the request has be aborted.
        /// </summary>
        CancellationToken RequestAborted { get; }

        /// <summary>
        /// Gets the field helper for collection fields
        /// and creating a field middleware.
        /// </summary>
        /// <value></value>
        IFieldHelper FieldHelper { get; }

        /// <summary>
        /// Gets the activator helper class.
        /// </summary>
        IActivator Activator { get; }
    }
}
