
using System;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class RequestCoreMiddlewareContext
        : IRequestCoreMiddlewareContext
    {
        public RequestCoreMiddlewareContext(
            NameString schemaName,
            IServiceProvider services,
            IActivator activator,
            IErrorHandler errorHandler,
            IRequestExecutorOptionsAccessor options)
        {
            SchemaName = schemaName;
            Services = services;
            Activator = activator;
            ErrorHandler = errorHandler;
            Options = options;
        }

        public NameString SchemaName { get; }

        public IServiceProvider Services { get; }

        public IActivator Activator { get; }

        public IErrorHandler ErrorHandler { get; }

        public IRequestExecutorOptionsAccessor Options { get; }
    }
}