using HotChocolate.Execution.Processing;
using ModelContextProtocol.Protocol;

namespace HotChocolate.ModelContextProtocol;

internal sealed record GraphQLMcpTool
{
    public GraphQLMcpTool(IOperation operation, Tool tool)
    {
        Operation = operation;
        Tool = tool;
    }

    public string Name => Tool.Name;

    public IOperation Operation { get; init; }

    public Tool Tool { get; init; }

    public void Deconstruct(out IOperation operation, out Tool tool)
    {
        operation = Operation;
        tool = Tool;
    }
}
