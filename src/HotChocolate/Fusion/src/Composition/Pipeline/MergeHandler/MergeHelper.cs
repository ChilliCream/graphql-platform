using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal static class MergeHelper
{
    public static T GetOrCreateType<T>(Schema fusionGraph, string typeName) 
        where T : INamedType, INamedTypeSystemMember<T>
    {
        if (!fusionGraph.Types.TryGetType(typeName, out T? type))
        {
            type = T.Create(typeName);
            fusionGraph.Types.Add(type);
        }

        return type;
    }

    public static IType GetOrCreateType(Schema fusionGraph, IType type)
        => type.ReplaceNameType(n => GetOrCreateType(fusionGraph, n.Name, n.Kind));
    
    public static INamedType GetOrCreateType(Schema fusionGraph, string typeName, TypeKind kind) 
    {
        if (!fusionGraph.Types.TryGetType(typeName, out var type))
        {
            type = kind switch
            {
                TypeKind.Interface => InterfaceType.Create(typeName),
                TypeKind.Object => ObjectType.Create(typeName),
                TypeKind.Union => UnionType.Create(typeName),
                TypeKind.InputObject => InputObjectType.Create(typeName),
                TypeKind.Enum => EnumType.Create(typeName),
                TypeKind.Scalar => ScalarType.Create(typeName),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };

            fusionGraph.Types.Add(type);
        }

        return type;
    }
}