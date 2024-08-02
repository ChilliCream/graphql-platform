using HotChocolate.Execution.Options;

namespace HotChocolate.Execution;

internal sealed class RequestCoreMiddlewareContext : IRequestCoreMiddlewareContext
{
    public RequestCoreMiddlewareContext(
        string schemaName,
        IServiceProvider services,
        IServiceProvider schemaServices,
        IRequestExecutorOptionsAccessor options)
    {
        SchemaName = schemaName;
        Services = services;
        SchemaServices = schemaServices;
        Options = options;
    }

    public string SchemaName { get; }

    public IServiceProvider Services { get; }

    public IServiceProvider SchemaServices { get; }

    public IRequestExecutorOptionsAccessor Options { get; }
}
