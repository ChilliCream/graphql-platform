using System.Collections.Generic;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class SchemaInfo
{
    public NameString Name { get; set; } = Schema.DefaultName;
    
    public ObjectTypeInfo? Query { get; set; }

    public ObjectTypeInfo? Mutation { get; set; }

    public ObjectTypeInfo? Subscription { get; set; }

    public Dictionary<string, ITypeInfo> Types { get; } =
        new Dictionary<string, ITypeInfo>();
}
