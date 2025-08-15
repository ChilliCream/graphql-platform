using HotChocolate.Execution.Processing;
using ModelContextProtocol.Protocol;

namespace HotChocolate.ModelContextProtocol;

internal sealed record GraphQLMcpTool(IOperation Operation, Tool McpTool)
{
    public string Name => McpTool.Name;
}
