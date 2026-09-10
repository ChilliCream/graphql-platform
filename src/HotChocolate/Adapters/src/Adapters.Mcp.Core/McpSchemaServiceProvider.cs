namespace HotChocolate.Adapters.Mcp;

/// <summary>
/// Provides the schema-scoped services that the MCP request handlers resolve from.
/// </summary>
/// <remarks>
/// The provider is bound to the schema services when the MCP server options are built.
/// </remarks>
internal sealed class McpSchemaServiceProvider : IServiceProvider
{
    private IServiceProvider? _schemaServices;

    public void Bind(IServiceProvider schemaServices)
    {
        ArgumentNullException.ThrowIfNull(schemaServices);

        _schemaServices = schemaServices;
    }

    public object? GetService(Type serviceType) => _schemaServices?.GetService(serviceType);
}
