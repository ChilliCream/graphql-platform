using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.ModelContextProtocol.Registries;

internal sealed class GraphQLMcpToolRegistry
{
    private readonly Dictionary<string, GraphQLMcpTool> _tools = [];

    public void Add(GraphQLMcpTool tool)
    {
        _tools[tool.Name] = tool;
    }

    public Dictionary<string, GraphQLMcpTool> GetTools()
    {
        return _tools;
    }

    public bool TryGetTool(string name, [NotNullWhen(true)] out GraphQLMcpTool? tool)
    {
        return _tools.TryGetValue(name, out tool);
    }

    public void Clear()
    {
        _tools.Clear();
    }
}
