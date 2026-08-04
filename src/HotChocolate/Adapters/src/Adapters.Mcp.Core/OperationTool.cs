using HotChocolate.Language;
using ModelContextProtocol.Protocol;

namespace HotChocolate.Adapters.Mcp;

internal sealed class OperationTool(DocumentNode documentNode, Tool tool)
{
    public string Name => Tool.Name;

    public DocumentNode DocumentNode { get; } = documentNode;

    public Tool Tool { get; } = tool;

    public Resource? ViewResource { get; init; }

    public string? ViewHtml { get; init; }

    /// <summary>
    /// True when the tool's document validates against the current schema. Invalid tools are
    /// still listed (so consumers see they exist) but calls to them must be rejected.
    /// </summary>
    public bool HasValidDocument { get; init; } = true;
}
