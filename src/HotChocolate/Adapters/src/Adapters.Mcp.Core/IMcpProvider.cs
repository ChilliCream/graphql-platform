using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Configuration;

namespace HotChocolate.Adapters.Mcp;

internal interface IMcpProvider
{
    ImmutableArray<string> SchemaNames { get; }

    McpSetup GetSetup(string? schemaName = null);
}
