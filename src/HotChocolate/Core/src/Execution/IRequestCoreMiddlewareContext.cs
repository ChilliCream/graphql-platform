
using System;
using HotChocolate.Execution.Options;

namespace HotChocolate.Execution
{
    /// <summary>
    /// This context is available when create a middleware pipeline.
    /// </summary>
    public interface IRequestCoreMiddlewareContext
    {
        /// <summary>
        /// Gets the schema name.
        /// </summary>
        NameString SchemaName { get; }

        /// <summary>
        /// Gets the application level service provider.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets the schema level service provider.
        /// </summary>
        IServiceProvider SchemaServices { get; }

        /// <summary>
        /// Gets the executor options.
        /// </summary>
        IRequestExecutorAnalyzerOptionsAccessor AnalyzerOptions { get; }
    }
}