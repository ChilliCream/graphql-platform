using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Configuration;

namespace HotChocolate.Adapters.Mcp;

internal interface IMcpProvider
{
    ImmutableArray<string> Names { get; }

    McpSetup GetSetup(string? name = null);
}
