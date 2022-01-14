using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.SchemaBuilding;

internal sealed class DefaultGatewayBuilder : IGatewayBuilder
{
    public DefaultGatewayBuilder(IRequestExecutorBuilder builder)
        => Builder = builder;

    public NameString Name => Builder.Name;

    public IServiceCollection Services => Builder.Services;

    public IRequestExecutorBuilder Builder { get; }
}
