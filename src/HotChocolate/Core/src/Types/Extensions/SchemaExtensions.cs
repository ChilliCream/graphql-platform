using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using TypeThrowHelper = HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate;

/// <summary>
/// Provides extension methods to <see cref="Schema"/>.
/// </summary>
public static class SchemaExtensions
{
    /// <summary>
    /// Get the root operation object type.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <param name="operation">The operation type.</param>
    /// <returns>
    /// Returns the root operation object type.
    /// </returns>
    public static ObjectType? GetOperationType(this Schema schema, OperationType operation)
        => operation switch
        {
            OperationType.Query => schema.QueryType,
            OperationType.Mutation => schema.MutationType,
            OperationType.Subscription => schema.SubscriptionType,
            _ => throw new NotSupportedException()
        };

    /// <summary>
    /// Tries to resolve a <see cref="ITypeSystemMember"/> by its <see cref="SchemaCoordinate"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema on which the <paramref name="member"/> shall be resolved.
    /// </param>
    /// <param name="coordinateString">
    /// A string representing a schema coordinate.
    /// </param>
    /// <param name="member">
    /// The resolved type system member.
    /// </param>
    /// <returns>
    /// <c>true</c> if a type system member was found with the given
    /// <paramref name="coordinateString"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetMember(
        this Schema schema,
        string coordinateString,
        [NotNullWhen(true)] out ITypeSystemMember? member)
    {
        if (SchemaCoordinate.TryParse(coordinateString, out var coordinate))
        {
            return TryGetMember(schema, coordinate.Value, out member);
        }

        member = null;
        return false;
    }

    /// <summary>
    /// Tries to resolve a <see cref="ITypeSystemMember"/> by its <see cref="SchemaCoordinate"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema on which the <paramref name="member"/> shall be resolved.
    /// </param>
    /// <param name="coordinate">
    /// A schema coordinate.
    /// </param>
    /// <param name="member">
    /// The resolved type system member.
    /// </param>
    /// <returns>
    /// <c>true</c> if a type system member was found with the given
    /// <paramref name="coordinate"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetMember(
        this Schema schema,
        SchemaCoordinate coordinate,
        [NotNullWhen(true)] out ITypeSystemMember? member)
    {
        if (coordinate.OfDirective)
        {
            if (schema.DirectiveTypes.TryGetDirective(coordinate.Name, out var directive))
            {
                if (coordinate.ArgumentName is null)
                {
                    member = directive;
                    return true;
                }

                if (directive.Arguments.TryGetField(coordinate.ArgumentName, out var arg))
                {
                    member = arg;
                    return true;
                }
            }

            member = null;
            return false;
        }

        if (schema.Types.TryGetType(coordinate.Name, out var type))
        {
            if (coordinate.MemberName is null)
            {
                member = type;
                return true;
            }

            if (coordinate.ArgumentName is null)
            {
                if (type.Kind is TypeKind.Enum)
                {
                    var enumType = type.ExpectEnumType();
                    if (enumType.Values.TryGetValue(coordinate.MemberName, out var enumValue))
                    {
                        member = enumValue;
                        return true;
                    }
                }

                if (type.Kind is TypeKind.InputObject)
                {
                    var inputType = type.ExpectInputObjectType();
                    if (inputType.Fields.TryGetField(coordinate.MemberName, out var input))
                    {
                        member = input;
                        return true;
                    }
                }
            }

            if (type.Kind is not TypeKind.Object and not TypeKind.Interface)
            {
                member = null;
                return false;
            }

            var complexType = type.ExpectComplexType();
            if (complexType.Fields.TryGetField(coordinate.MemberName, out var field))
            {
                if (coordinate.ArgumentName is null)
                {
                    member = field;
                    return true;
                }

                if (field.Arguments.TryGetField(coordinate.ArgumentName, out var fieldArg))
                {
                    member = fieldArg;
                    return true;
                }
            }
        }

        member = null;
        return false;
    }

    /// <summary>
    /// Gets a <see cref="ITypeSystemMember"/> by its <see cref="SchemaCoordinate"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema on which the member shall be resolved.
    /// </param>
    /// <param name="coordinateString">
    /// A string representing a schema coordinate.
    /// </param>
    /// <returns>
    /// Returns the resolved type system member.
    /// </returns>
    /// <exception cref="SyntaxException">
    /// The <paramref name="coordinateString"/> has invalid syntax.
    /// </exception>
    /// <exception cref="InvalidSchemaCoordinateException">
    /// Unable to resolve a type system member with the
    /// specified <paramref name="coordinateString"/>.
    /// </exception>
    public static ITypeSystemMember GetMember(
        this Schema schema,
        string coordinateString)
        => GetMember(schema, SchemaCoordinate.Parse(coordinateString));

    /// <summary>
    /// Gets a <see cref="ITypeSystemMember"/> by its <see cref="SchemaCoordinate"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema on which the member shall be resolved.
    /// </param>
    /// <param name="coordinate">
    /// A schema coordinate.
    /// </param>
    /// <returns>
    /// Returns the resolved type system member.
    /// </returns>
    /// <exception cref="InvalidSchemaCoordinateException">
    /// Unable to resolve a type system member with the
    /// specified <paramref name="coordinate"/>.
    /// </exception>
    public static ITypeSystemMember GetMember(
        this Schema schema,
        SchemaCoordinate coordinate)
    {
        if (coordinate.OfDirective)
        {
            if (schema.DirectiveTypes.TryGetDirective(coordinate.Name, out var directive))
            {
                if (coordinate.ArgumentName is null)
                {
                    return directive;
                }

                if (directive.Arguments.TryGetField(coordinate.ArgumentName, out var arg))
                {
                    return arg;
                }

                throw TypeThrowHelper.Schema_GetMember_DirectiveArgumentNotFound(coordinate);
            }

            throw TypeThrowHelper.Schema_GetMember_DirectiveNotFound(coordinate);
        }

        if (schema.Types.TryGetType(coordinate.Name, out var type))
        {
            if (coordinate.MemberName is null)
            {
                return type;
            }

            if (coordinate.ArgumentName is null)
            {
                if (type.Kind is TypeKind.Enum)
                {
                    var enumType = (EnumType)type;
                    if (enumType.TryGetValue(coordinate.MemberName, out var enumValue))
                    {
                        return enumValue;
                    }

                    throw TypeThrowHelper.Schema_GetMember_EnumValueNotFound(coordinate);
                }

                if (type.Kind is TypeKind.InputObject)
                {
                    var inputType = (InputObjectType)type;
                    if (inputType.Fields.TryGetField(coordinate.MemberName, out var input))
                    {
                        return input;
                    }

                    throw TypeThrowHelper.Schema_GetMember_InputFieldNotFound(coordinate);
                }
            }

            if (type.Kind is not TypeKind.Object and not TypeKind.Interface)
            {
                throw TypeThrowHelper.Schema_GetMember_InvalidCoordinate(coordinate, type);
            }

            var complexType = type.ExpectComplexType();
            if (complexType.Fields.TryGetField(coordinate.MemberName, out var field))
            {
                if (coordinate.ArgumentName is null)
                {
                    return field;
                }

                if (field.Arguments.TryGetField(coordinate.ArgumentName, out var fieldArg))
                {
                    return fieldArg;
                }

                throw TypeThrowHelper.Schema_GetMember_FieldArgNotFound(coordinate);
            }

            throw TypeThrowHelper.Schema_GetMember_FieldNotFound(coordinate);
        }

        throw TypeThrowHelper.Schema_GetMember_TypeNotFound(coordinate);
    }

    /// <summary>
    /// Gets the <see cref="IReadOnlySchemaOptions"/> of the <paramref name="schema"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema to get the options for.
    /// </param>
    /// <returns>
    /// The schema options.
    /// </returns>
    internal static IReadOnlySchemaOptions GetOptions(this ISchemaDefinition schema)
    {
        return schema.Features.GetRequired<IReadOnlySchemaOptions>();
    }
}
