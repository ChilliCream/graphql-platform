using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class TypeDefinitionExtensions
{
    // todo put all of this data in the execution schema.
    public static IEnumerable<MutableObjectTypeDefinition> GetPossibleTypes(
        this ITypeDefinition type,
        string schemaName,
        MutableSchemaDefinition schema)
    {
        if (type.Kind is not TypeKind.Object and not TypeKind.Interface and not TypeKind.Union)
        {
            throw new ArgumentException(
                "The specified type is not an abstract type.", // tmp not very accurate message + loc
                nameof(type));
        }

        switch (type)
        {
            case MutableObjectTypeDefinition objectType:
                yield return objectType;
                break;

            case MutableInterfaceTypeDefinition interfaceType:
                foreach (var possibleType in schema.Types)
                {
                    if (possibleType is not MutableObjectTypeDefinition objectType)
                    {
                        continue;
                    }

                    var implementedInSchema = objectType.Directives[FusionImplements]
                        .Any(d => (string)d.Arguments[Schema].Value! == schemaName
                            && (string)d.Arguments[Interface].Value! == interfaceType.Name);

                    if (implementedInSchema)
                    {
                        yield return objectType;
                    }
                }

                break;

            case MutableUnionTypeDefinition unionType:
                var memberTypeNamesInSchema =
                    unionType
                        .Directives[FusionUnionMember]
                        .Where(d => (string)d.Arguments[Schema].Value! == schemaName)
                        .Select(d => (string)d.Arguments[Member].Value!);

                foreach (var memberType in memberTypeNamesInSchema)
                {
                    yield return (MutableObjectTypeDefinition)schema.Types[memberType];
                }

                break;
        }
    }
}
