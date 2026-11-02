using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

internal static class TypeDefinitionExtensions
{
    public static bool ExistsInSchema(this ITypeDefinition type, string schemaName)
    {
        return type switch
        {
            FusionObjectTypeDefinition objectType => objectType.Sources.ContainsSchema(schemaName),
            FusionInterfaceTypeDefinition interfaceType => interfaceType.Sources.ContainsSchema(schemaName),
            FusionUnionTypeDefinition unionType => unionType.Sources.ContainsSchema(schemaName),
            _ => false
        };
    }
}
