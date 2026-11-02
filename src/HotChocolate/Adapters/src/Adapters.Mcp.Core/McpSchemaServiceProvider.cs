namespace HotChocolate.Adapters.Mcp;

/// <summary>
/// Provides the schema-scoped services that the MCP request handlers resolve from.
/// </summary>
/// <remarks>
/// The provider is bound to the schema services the first time it is resolved from them.
/// </remarks>
internal sealed class McpSchemaServiceProvider : IServiceProvider
{
    private IServiceProvider? _schemaServices;

    public void Bind(IServiceProvider schemaServices)
    {
        ArgumentNullException.ThrowIfNull(schemaServices);

        _schemaServices = schemaServices;
    }

    public object? GetService(Type serviceType)
    {
        if (_schemaServices is null)
        {
            throw new InvalidOperationException("The MCP schema services have not been bound.");
        }

        return _schemaServices.GetService(serviceType);
    }
}
