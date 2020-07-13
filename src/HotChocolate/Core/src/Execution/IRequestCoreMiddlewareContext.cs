
using System;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Utilities;

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
        /// Gets the internal activator that is used to create instances of objects.
        /// </summary>
        IActivator Activator { get; }

        /// <summary>
        /// Gets the error handler.
        /// </summary>
        IErrorHandler ErrorHandler { get; }

        /// <summary>
        /// Gets the executor options.
        /// </summary>
        IRequestExecutorOptionsAccessor Options { get; }
    }
}