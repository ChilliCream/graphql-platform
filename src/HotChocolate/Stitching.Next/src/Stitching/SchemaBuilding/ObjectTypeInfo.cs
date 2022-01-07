using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.SchemaBuilding;

internal sealed class ObjectTypeInfo : ITypeInfo
{
    public ObjectTypeInfo(ObjectTypeDefinitionNode definition)
    {
        Name = definition.Name.Value;
        Definition = definition;
    }

    public NameString Name { get; }

    public TypeKind Kind => TypeKind.Object;

    public ObjectTypeDefinitionNode Definition { get; set; }

    ITypeDefinitionNode ITypeInfo.Definition => Definition;

    public IList<ObjectFetcherInfo> Fetchers { get; } =
        new List<ObjectFetcherInfo>();
}
