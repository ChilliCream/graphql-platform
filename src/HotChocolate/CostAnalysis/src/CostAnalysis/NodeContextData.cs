using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

internal sealed class NodeContextData : Dictionary<ISyntaxNode, Dictionary<string, int>>
{
    public void Set(ISyntaxNode node, string key, int value)
    {
        if (!TryGetValue(node, out var contextData))
        {
            contextData = new Dictionary<string, int>();
            this[node] = contextData;
        }

        this[node][key] = value;
    }

    public int Get(ISyntaxNode node, string key)
    {
        if (TryGetValue(node, out var contextData) && contextData.TryGetValue(key, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException();
    }

    public bool TryGet(ISyntaxNode node, string key, out int value)
    {
        if (TryGetValue(node, out var contextData) && contextData.TryGetValue(key, out var v))
        {
            value = v;

            return true;
        }

        value = default;

        return false;
    }

    public void Remove(ISyntaxNode node, string key)
    {
        if (TryGetValue(node, out var contextData))
        {
            contextData.Remove(key);
        }
    }
}
