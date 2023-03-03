using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Metadata;

internal sealed class MemberBindingCollection : IEnumerable<MemberBinding>
{
    private readonly Dictionary<string, MemberBinding> _bindings;

    public MemberBindingCollection(IEnumerable<MemberBinding> bindings)
    {
        _bindings = bindings.ToDictionary(t => t.SchemaName, StringComparer.Ordinal);
    }

    public int Count => _bindings.Count;

    public MemberBinding this[string subGraph] => _bindings[subGraph];

    public bool TryGetValue(string subGraph, [NotNullWhen(true)] out MemberBinding? value)
        => _bindings.TryGetValue(subGraph, out value);

    public bool ContainsSubGraph(string subGraph) => _bindings.ContainsKey(subGraph);

    public IEnumerator<MemberBinding> GetEnumerator() => _bindings.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static MemberBindingCollection Empty { get; } =
        new(new List<MemberBinding>());
}
