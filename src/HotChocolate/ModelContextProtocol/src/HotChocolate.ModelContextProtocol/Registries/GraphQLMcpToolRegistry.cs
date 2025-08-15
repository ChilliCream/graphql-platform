using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Utilities;

namespace HotChocolate.ModelContextProtocol.Registries;

internal sealed class GraphQLMcpToolRegistry
{
    private ImmutableDictionary<string, GraphQLMcpTool> _tools = ImmutableDictionary<string, GraphQLMcpTool>.Empty;
    private ImmutableArray<Func<Task>> _callbacks = [];

    public void OnToolsUpdate(Func<Task> callback)
        => _callbacks = _callbacks.Add(callback);

    public void UpdateTools(ImmutableDictionary<string, GraphQLMcpTool> tools)
    {
        _tools = tools;

        foreach (var callback in _callbacks)
        {
            callback().FireAndForget();
        }
    }

    public IEnumerable<GraphQLMcpTool> GetTools()
        => _tools.Values.OrderBy(t => t.Name);

    public bool TryGetTool(string name, [NotNullWhen(true)] out GraphQLMcpTool? tool)
        => _tools.TryGetValue(name, out tool);
}
