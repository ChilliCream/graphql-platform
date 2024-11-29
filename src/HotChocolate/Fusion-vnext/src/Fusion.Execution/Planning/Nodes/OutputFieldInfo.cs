using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning.Nodes;

public class OutputFieldInfo(string name, ICompositeType type, ImmutableArray<string> sources)
{
    public OutputFieldInfo(CompositeOutputField field)
        : this(field.Name, field.Type, field.Sources.Schemas)
    {

    }

    public string Name => name;

    public ICompositeType Type => type;

    public ImmutableArray<string> Sources => sources;
}
