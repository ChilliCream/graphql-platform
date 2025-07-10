using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public abstract class FusionFieldDefinitionCollection<TField>
    : IEnumerable<TField> where TField : IFieldDefinition
{
    private readonly TField[] _fields;
    private readonly FrozenDictionary<string, TField> _map;

    protected FusionFieldDefinitionCollection(TField[] fields)
    {
        _map = fields.ToFrozenDictionary(t => t.Name);
        _fields = fields;
    }

    public int Count => _map.Count;

    public TField this[string name] => _map[name];

    public TField this[int index] => _fields[index];

    public bool TryGetField(string name, [NotNullWhen(true)] out TField? field)
        => _map.TryGetValue(name, out field);

    public bool ContainsName(string name)
        => _map.ContainsKey(name);

    public IEnumerable<TField> AsEnumerable()
        => Unsafe.As<IEnumerable<TField>>(_fields);

    public IEnumerator<TField> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
