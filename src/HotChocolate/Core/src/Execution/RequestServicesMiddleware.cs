using System;
using HotChocolate.Execution.Options;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Defines request middleware that can be added to the GraphQL request pipeline.
    /// </summary>
    public delegate RequestDelegate RequestCoreMiddleware(
        IRequestCoreMiddlewareContext context,
        RequestDelegate next);

    public interface IRequestCoreMiddlewareContext
    {
        /// <summary>
        /// Gets the name of the request executor configuration.
        /// </summary>
        string Name { get; }

        IServiceProvider services { get; }

        IRequestExecutorOptionsAccessor options { get; }
    }
}
