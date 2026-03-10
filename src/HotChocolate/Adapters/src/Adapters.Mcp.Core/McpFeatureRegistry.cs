using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Protocol;

namespace HotChocolate.Adapters.Mcp;

internal sealed class McpFeatureRegistry
{
    private FrozenDictionary<string, (Prompt, ImmutableArray<PromptMessage>)> _prompts
        = FrozenDictionary<string, (Prompt, ImmutableArray<PromptMessage>)>.Empty;

    private FrozenDictionary<string, OperationTool> _tools
        = FrozenDictionary<string, OperationTool>.Empty;

    private FrozenDictionary<string, OperationTool> _toolsByMcpAppViewResourceUri
        = FrozenDictionary<string, OperationTool>.Empty;

    public void UpdatePrompts(ImmutableDictionary<string, (Prompt, ImmutableArray<PromptMessage>)> prompts)
    {
        _prompts = prompts.ToFrozenDictionary();
    }

    public void UpdateTools(ImmutableDictionary<string, OperationTool> tools)
    {
        _tools = tools.ToFrozenDictionary();
        _toolsByMcpAppViewResourceUri =
            tools.Values
                .Where(t => t.ViewResource is not null)
                .ToFrozenDictionary(t => t.ViewResource!.Uri);
    }

    public IEnumerable<(Prompt, ImmutableArray<PromptMessage>)> GetPrompts()
        => _prompts.Values.OrderBy(p => p.Item1.Name);

    public bool TryGetPrompt(
        string name,
        [NotNullWhen(true)] out (Prompt, ImmutableArray<PromptMessage>)? prompt)
    {
        if (_prompts.TryGetValue(name, out var result))
        {
            prompt = result;
            return true;
        }

        prompt = null;
        return false;
    }

    public IEnumerable<OperationTool> GetTools()
        => _tools.Values.OrderBy(t => t.Name);

    public bool TryGetTool(string name, [NotNullWhen(true)] out OperationTool? tool)
        => _tools.TryGetValue(name, out tool);

    public bool TryGetToolByViewResourceUri(
        string uri,
        [NotNullWhen(true)] out OperationTool? tool)
        => _toolsByMcpAppViewResourceUri.TryGetValue(uri, out tool);
}
