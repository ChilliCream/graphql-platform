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

    public MemberBinding this[string schemaName] => _bindings[schemaName];

    public bool TryGetValue(string schemaName, [NotNullWhen(true)] out MemberBinding? value)
        => _bindings.TryGetValue(schemaName, out value);

    public bool ContainsSchema(string schemaName) => _bindings.ContainsKey(schemaName);

    public IEnumerator<MemberBinding> GetEnumerator() => _bindings.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static MemberBindingCollection Empty { get; } =
        new(new List<MemberBinding>());
}
