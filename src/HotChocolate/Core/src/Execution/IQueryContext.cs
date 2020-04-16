﻿using System;
using System.Threading;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface IQueryContext : IHasContextData
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
        /// Gets or sets the scoped request services.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets or sets the parsed query document.
        /// </summary>
        DocumentNode Document { get; set; }

        /// <summary>
        /// Notifies when the connection underlying this request is aborted
        /// and thus request operations should be cancelled.
        /// </summary>
        CancellationToken RequestAborted { get; set; }

        /// <summary>
        /// Gets or sets an unexpected execution exception.
        /// </summary>
        Exception Exception { get; set; }
    }
}
