using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Utilities;

namespace HotChocolate.ModelContextProtocol;

internal sealed class ToolRegistry
{
    private ImmutableDictionary<string, OperationTool> _tools = ImmutableDictionary<string, OperationTool>.Empty;
    private ImmutableArray<Func<Task>> _callbacks = [];

    public void OnToolsUpdate(Func<Task> callback)
        => _callbacks = _callbacks.Add(callback);

    public void UpdateTools(ImmutableDictionary<string, OperationTool> tools)
    {
        _tools = tools;

        foreach (var callback in _callbacks)
        {
            callback().FireAndForget();
        }
    }

    public IEnumerable<OperationTool> GetTools()
        => _tools.Values.OrderBy(t => t.Name);

    public bool TryGetTool(string name, [NotNullWhen(true)] out OperationTool? tool)
        => _tools.TryGetValue(name, out tool);
}
