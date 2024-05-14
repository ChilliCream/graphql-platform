using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class Schema : IHasDirectives, IHasContextData, INamedTypeSystemMember<Schema>
{
    public string Name { get; set; } = "default";

    public string? Description { get; set; }

    public ObjectType? QueryType { get; set; }

    public ObjectType? MutationType { get; set; }

    public ObjectType? SubscriptionType { get; set; }

    public TypeCollection Types { get; } = [];

    public DirectiveTypeCollection DirectiveTypes { get; } = [];

    public DirectiveCollection Directives { get; } = [];

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    /// <summary>
    /// Tries to resolve a <see cref="ITypeSystemMember"/> by its <see cref="SchemaCoordinate"/>.
    /// </summary>
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
    public bool TryGetMember<T>(
        SchemaCoordinate coordinate,
        [NotNullWhen(true)] out T? member)
        where T : ITypeSystemMember
    {
        if (TryGetMember(coordinate, out var m) && m is T casted)
        {
            member = casted;
            return true;
        }

        member = default;
        return false;
    }

    /// <summary>
    /// Tries to resolve a <see cref="ITypeSystemMember"/> by its <see cref="SchemaCoordinate"/>.
    /// </summary>
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
    public bool TryGetMember(
        SchemaCoordinate coordinate,
        [NotNullWhen(true)] out ITypeSystemMember? member)
    {
        if (coordinate.OfDirective)
        {
            if (DirectiveTypes.TryGetDirective(coordinate.Name, out var directive))
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

        if (Types.TryGetType(coordinate.Name, out var type))
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
                    var enumType = (EnumType)type;
                    if (enumType.Values.TryGetValue(coordinate.MemberName, out var enumValue))
                    {
                        member = enumValue;
                        return true;
                    }
                }

                if (type.Kind is TypeKind.InputObject)
                {
                    var inputType = (InputObjectType)type;
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

            var complexType = (ComplexType)type;
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

    public static Schema Create(string name) => new() { Name = name, };
}
