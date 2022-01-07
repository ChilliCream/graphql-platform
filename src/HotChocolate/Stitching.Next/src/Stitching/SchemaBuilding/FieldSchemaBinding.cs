using System.Collections.Generic;

namespace HotChocolate.Stitching.SchemaBuilding;

internal readonly struct FieldSchemaBinding
{
    public FieldSchemaBinding(NameString source, IReadOnlyList<string> fields)
    {
        Source = source;
        Fields = fields;
    }

    public NameString Source { get; }

    public IReadOnlyList<string> Fields { get; }
}
