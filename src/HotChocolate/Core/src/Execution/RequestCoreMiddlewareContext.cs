
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
            IRequestExecutorAnalyzerOptionsAccessor analyzerOptions)
        {
            SchemaName = schemaName;
            Services = services;
            SchemaServices = schemaServices;
            AnalyzerOptions = analyzerOptions;
        }

        public NameString SchemaName { get; }

        public IServiceProvider Services { get; }

        public IServiceProvider SchemaServices { get; }

        public IRequestExecutorAnalyzerOptionsAccessor AnalyzerOptions { get; }
    }
}