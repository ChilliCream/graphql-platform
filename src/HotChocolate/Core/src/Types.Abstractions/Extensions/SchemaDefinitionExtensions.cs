using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate;

/// <summary>
/// Provides extension methods for <see cref="ISchemaDefinition"/>.
/// </summary>
public static class SchemaDefinitionExtensions
{
    /// <summary>
    /// Resolves a <see cref="ITypeSystemMember"/> by its <see cref="SchemaCoordinate"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema definition to resolve against.
    /// </param>
    /// <param name="coordinate">
    /// The schema coordinate to resolve.
    /// </param>
    /// <returns>
    /// The resolved type system member.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no type system member exists for the given <paramref name="coordinate"/>.
    /// </exception>
    public static ITypeSystemMember GetMember(
        this ISchemaDefinition schema,
        SchemaCoordinate coordinate)
    {
        if (!schema.TryGetMember(coordinate, out var member))
        {
            throw new InvalidOperationException(
                $"Failed to resolve schema coordinate '{coordinate}'.");
        }

        return member;
    }

    /// <summary>
    /// Tries to resolve a <see cref="ITypeSystemMember"/> by its <see cref="SchemaCoordinate"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema definition to resolve against.
    /// </param>
    /// <param name="coordinate">
    /// The schema coordinate to resolve.
    /// </param>
    /// <param name="member">
    /// The resolved type system definition.
    /// </param>
    /// <returns>
    /// <c>true</c> if a type system definition was found with the given
    /// <paramref name="coordinate"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetMember(
        this ISchemaDefinition schema,
        SchemaCoordinate coordinate,
        [NotNullWhen(true)] out ITypeSystemMember? member)
    {
        if (coordinate.OfDirective)
        {
            if (!schema.DirectiveDefinitions.TryGetDirective(coordinate.Name, out var directive))
            {
                member = null;
                return false;
            }

            if (coordinate.ArgumentName is not null)
            {
                if (directive.Arguments.TryGetField(coordinate.ArgumentName, out var arg))
                {
                    member = arg;
                    return true;
                }

                member = null;
                return false;
            }

            member = directive;
            return true;
        }

        if (!schema.Types.TryGetType(coordinate.Name, out var type))
        {
            member = null;
            return false;
        }

        if (coordinate.MemberName is null)
        {
            member = type;
            return true;
        }

        switch (type)
        {
            case IComplexTypeDefinition complexType:
                if (!complexType.Fields.TryGetField(coordinate.MemberName, out var field))
                {
                    member = null;
                    return false;
                }

                if (coordinate.ArgumentName is not null)
                {
                    if (field.Arguments.TryGetField(coordinate.ArgumentName, out var fieldArg))
                    {
                        member = fieldArg;
                        return true;
                    }

                    member = null;
                    return false;
                }

                member = field;
                return true;

            case IEnumTypeDefinition enumType:
                if (enumType.Values.TryGetValue(coordinate.MemberName, out var enumValue))
                {
                    member = enumValue;
                    return true;
                }

                member = null;
                return false;

            case IInputObjectTypeDefinition inputType:
                if (inputType.Fields.TryGetField(coordinate.MemberName, out var inputField))
                {
                    member = inputField;
                    return true;
                }

                member = null;
                return false;

            default:
                member = null;
                return false;
        }
    }
}
