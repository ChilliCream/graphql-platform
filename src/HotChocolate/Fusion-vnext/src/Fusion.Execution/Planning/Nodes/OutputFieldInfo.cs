using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning.Nodes;

public class OutputFieldInfo
{
    private readonly string _name;
    private readonly ICompositeType _type;
    private readonly ImmutableArray<string> _sources;

    public OutputFieldInfo(CompositeOutputField field)
        : this(field.Name, field.Type, field.Sources.Schemas)
    {

    }

    public OutputFieldInfo(string name, ICompositeType type, ImmutableArray<string> sources)
    {
        _name = name;
        _type = type;
        _sources = sources;
    }

    public string Name => _name;

    public ICompositeType Type => _type;

    public ImmutableArray<string> Sources => _sources;
}
