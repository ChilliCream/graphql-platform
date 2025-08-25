using HotChocolate.Language;
using ModelContextProtocol.Protocol;

namespace HotChocolate.ModelContextProtocol;

internal sealed class OperationTool(DocumentNode documentNode, Tool tool)
{
    public string Name => Tool.Name;

    public DocumentNode DocumentNode { get; } = documentNode;

    public Tool Tool { get; } = tool;
}
