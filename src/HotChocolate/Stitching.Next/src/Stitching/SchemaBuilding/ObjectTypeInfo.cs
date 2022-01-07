using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.SchemaBuilding;

internal sealed class ObjectTypeInfo : ITypeInfo
{
    public ObjectTypeInfo(ObjectTypeDefinitionNode definition)
    {
        Name = definition.Name.Value;
        Kind = TypeKind.Object;
        Definition = definition;
    }

    public NameString Name { get; }

    public TypeKind Kind { get; }

    public ObjectTypeDefinitionNode Definition { get; }

    public IList<ObjectFetcherInfo> Fetchers { get; } =
        new List<ObjectFetcherInfo>();
}
