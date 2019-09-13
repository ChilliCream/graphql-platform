using System;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    public interface IQueryContext
        : IHasContextData
    {
        /// <summary>
        /// Gets the GraphQL schema on which the query is executed.
        /// </summary>
        ISchema Schema { get; }

        /// <summary>
        /// Gets or sets the initial query request.
        /// </summary>
        IReadOnlyQueryRequest Request { get; set; }

        /// <summary>
        /// Gets or sets a unique query key that can be used for caching.
        /// </summary>
        /// <returns></returns>
        string QueryKey { get; set; }

        /// <summary>
        /// Gets the request service scope.
        /// </summary>
        IRequestServiceScope ServiceScope { get; }

        /// <summary>
        /// Gets or sets the scoped request services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets or sets the parsed query document.
        /// </summary>
        DocumentNode Document { get; set; }

        /// <summary>
        /// Gets or sets the cached query.
        /// </summary>
        ICachedQuery CachedQuery { get; set; }

        /// <summary>
        /// Gets or sets the operation that shall be executed.
        /// </summary>
        IOperation Operation { get; set; }

        /// <summary>
        /// Gets or sets the query validation results.
        /// </summary>
        /// <returns></returns>
        QueryValidationResult ValidationResult { get; set; }

        /// <summary>
        /// Gets or sets the query execution result.
        /// </summary>
        IExecutionResult Result { get; set; }

        /// <summary>
        /// Notifies when the connection underlying this request is aborted
        /// and thus request operations should be cancelled.
        /// </summary>
        CancellationToken RequestAborted { get; set; }

        /// <summary>
        /// Gets or sets an unexpected execution exception.
        /// </summary>
        Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the execution bound field middleware resolver.
        /// </summary>
        Func<ObjectField, FieldNode, FieldDelegate> MiddlewareResolver
        { get; set; }
    }
}
