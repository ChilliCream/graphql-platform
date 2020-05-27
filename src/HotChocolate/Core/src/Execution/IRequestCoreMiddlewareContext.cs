
using System;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    public interface IRequestCoreMiddlewareContext
    {
        /// <summary>
        /// Gets the name of the request executor configuration.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the application level service provider.
        /// </summary>
        IServiceProvider Services { get; }

        IActivator Activator { get; }

        IErrorHandler ErrorHandler { get; }

        /// <summary>
        /// Gets the executor options.
        /// </summary>
        IRequestExecutorOptionsAccessor Options { get; }
    }
}