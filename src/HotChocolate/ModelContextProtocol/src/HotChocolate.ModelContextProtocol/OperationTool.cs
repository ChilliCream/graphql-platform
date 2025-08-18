using HotChocolate.Execution.Processing;
using ModelContextProtocol.Protocol;

namespace HotChocolate.ModelContextProtocol;

internal sealed class OperationTool(IOperation operation, Tool tool)
{
    public string Name => Tool.Name;

    public IOperation Operation { get; } = operation;

    public Tool Tool { get; } = tool;
}
