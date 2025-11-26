using HotChocolate.Language;
using ModelContextProtocol.Protocol;

namespace HotChocolate.Adapters.Mcp;

internal sealed class OperationTool(DocumentNode documentNode, Tool tool)
{
    public string Name => Tool.Name;

    public DocumentNode DocumentNode { get; } = documentNode;

    public Tool Tool { get; } = tool;

    public Resource? OpenAiComponentResource { get; init; }

    public string? OpenAiComponentHtml { get; init; }
}
