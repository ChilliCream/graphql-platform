
using System;
using HotChocolate.Execution.Options;

namespace HotChocolate.Execution
{
    internal sealed class RequestCoreMiddlewareContext
        : IRequestCoreMiddlewareContext
    {
        public RequestCoreMiddlewareContext(
            NameString schemaName,
            IServiceProvider services,
            IServiceProvider schemaServices,
            IRequestExecutorOptionsAccessor options)
        {
            SchemaName = schemaName;
            Services = services;
            SchemaServices = schemaServices;
            Options = options;
        }

        public NameString SchemaName { get; }

        public IServiceProvider Services { get; }

        public IServiceProvider SchemaServices { get; }

        public IRequestExecutorOptionsAccessor Options { get; }
    }
}