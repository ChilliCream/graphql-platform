using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

internal static class SchemaDefinitionTypeResolver
{
    public static IObjectTypeDefinition ResolveObjectType(
        ISchemaDefinition schema,
        object? runtimeValue)
    {
        var typeName = ResolveTypeName(runtimeValue);

        if (schema.Types.TryGetType(typeName, out var type)
            && type is IObjectTypeDefinition resolvedType)
        {
            return resolvedType;
        }

        throw new InvalidOperationException(
            $"The schema does not declare an object type named '{typeName}'.");
    }

    public static string ResolveTypeName(object? runtimeValue)
    {
        return runtimeValue switch
        {
            ITypeDefinition => "__Type",
            IOutputFieldDefinition => "__Field",
            IInputValueDefinition => "__InputValue",
            IEnumValue => "__EnumValue",
            IDirectiveDefinition => "__Directive",
            _ => throw new InvalidOperationException(
                "Cannot resolve a concrete introspection __typename for runtime value of type "
                + $"'{runtimeValue?.GetType().FullName ?? "null"}'.")
        };
    }
}
