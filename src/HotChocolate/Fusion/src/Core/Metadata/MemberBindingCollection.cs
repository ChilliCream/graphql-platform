using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Metadata;

internal sealed class MemberBindingCollection : IEnumerable<MemberBinding>
{
    private readonly Dictionary<string, MemberBinding> _bindings;

    public MemberBindingCollection(IEnumerable<MemberBinding> bindings)
    {
        if (bindings is null)
        {
            throw new ArgumentNullException(nameof(bindings));
        }

        _bindings = bindings.ToDictionary(t => t.SubgraphName, StringComparer.Ordinal);
    }

    public MemberBindingCollection(Dictionary<string, MemberBinding> bindings)
    {
        if (bindings is null)
        {
            throw new ArgumentNullException(nameof(bindings));
        }

        _bindings = bindings;
    }

    public int Count => _bindings.Count;

    public MemberBinding this[string subgraph] => _bindings[subgraph];

    public bool TryGetValue(string subgraph, [NotNullWhen(true)] out MemberBinding? value)
        => _bindings.TryGetValue(subgraph, out value);

    public bool ContainsSubgraph(string subgraph) => _bindings.ContainsKey(subgraph);

    public IEnumerator<MemberBinding> GetEnumerator() => _bindings.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static MemberBindingCollection Empty { get; } =
        new(new List<MemberBinding>());
}
