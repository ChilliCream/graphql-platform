using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

/// <summary>
/// Resolves a <see cref="SchemaCoordinate"/> to the corresponding
/// type system definition from a schema.
/// </summary>
internal static class SchemaCoordinateResolver
{
    /// <summary>
    /// Resolves the specified schema coordinate to a type system definition.
    /// </summary>
    /// <param name="schema">The schema to resolve against.</param>
    /// <param name="coordinate">The schema coordinate to resolve.</param>
    /// <returns>
    /// The resolved definition, or <c>null</c> if the coordinate
    /// does not match any element in the schema.
    /// </returns>
    public static object? Resolve(ISchemaDefinition schema, SchemaCoordinate coordinate)
    {
        if (coordinate.OfDirective)
        {
            if (!schema.DirectiveDefinitions.TryGetDirective(coordinate.Name, out var directive))
            {
                return null;
            }

            if (coordinate.ArgumentName is not null)
            {
                return directive.Arguments.TryGetField(coordinate.ArgumentName, out var arg)
                    ? arg
                    : null;
            }

            return directive;
        }

        if (!schema.Types.TryGetType(coordinate.Name, out var type))
        {
            return null;
        }

        if (coordinate.MemberName is null)
        {
            return type;
        }

        switch (type)
        {
            case IComplexTypeDefinition complexType:
                if (!complexType.Fields.TryGetField(coordinate.MemberName, out var field))
                {
                    return null;
                }

                if (coordinate.ArgumentName is not null)
                {
                    return field.Arguments.TryGetField(coordinate.ArgumentName, out var fieldArg)
                        ? fieldArg
                        : null;
                }

                return field;

            case IEnumTypeDefinition enumType:
                return enumType.Values.TryGetValue(coordinate.MemberName, out var enumValue)
                    ? enumValue
                    : null;

            case IInputObjectTypeDefinition inputType:
                return inputType.Fields.TryGetField(coordinate.MemberName, out var inputField)
                    ? inputField
                    : null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Gets the introspection type name for a resolved definition object
    /// within the <c>__SchemaDefinition</c> union.
    /// </summary>
    /// <param name="definition">The resolved definition object.</param>
    /// <returns>
    /// The introspection type name, or <c>null</c> if the definition
    /// is not a recognized member of the <c>__SchemaDefinition</c> union.
    /// </returns>
    public static string? GetTypeName(object definition)
    {
        return definition switch
        {
            ITypeDefinition => "__Type",
            IOutputFieldDefinition => "__Field",
            IInputValueDefinition => "__InputValue",
            IEnumValue => "__EnumValue",
            IDirectiveDefinition => "__Directive",
            _ => null
        };
    }
}
