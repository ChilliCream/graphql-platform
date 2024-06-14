using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL schema definition.
/// </summary>
public sealed class SchemaDefinition
    : INamedTypeSystemMemberDefinition<SchemaDefinition>
    , IDirectivesProvider
    , IFeatureProvider
{
    /// <inheritdoc />
    public string Name { get; set; } = "default";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the query type.
    /// </summary>
    public ObjectTypeDefinition? QueryType { get; set; }

    /// <summary>
    /// Gets or sets the mutation type.
    /// </summary>
    public ObjectTypeDefinition? MutationType { get; set; }

    /// <summary>
    /// Gets or sets the subscription type.
    /// </summary>
    public ObjectTypeDefinition? SubscriptionType { get; set; }

    /// <summary>
    /// Gets the types that are defined in this schema.
    /// </summary>
    public TypeCollection TypeDefinitions { get; } = [];

    /// <summary>
    /// Gets the directives that are defined in this schema.
    /// </summary>
    public DirectiveDefinitionCollection DirectiveDefinitions { get; } = [];

    /// <summary>
    /// Gets the directives that are annotated to this schema.
    /// </summary>
    public DirectiveCollection Directives { get; } = [];

    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <summary>
    /// Tries to resolve a <see cref="ITypeSystemMemberDefinition"/> by its <see cref="SchemaCoordinate"/>.
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
        where T : ITypeSystemMemberDefinition
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
    /// Tries to resolve a <see cref="ITypeSystemMemberDefinition"/> by its <see cref="SchemaCoordinate"/>.
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
        [NotNullWhen(true)] out ITypeSystemMemberDefinition? member)
    {
        if (coordinate.OfDirective)
        {
            if (DirectiveDefinitions.TryGetDirective(coordinate.Name, out var directive))
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

        if (TypeDefinitions.TryGetType(coordinate.Name, out var type))
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
                    var enumType = (EnumTypeDefinition)type;
                    if (enumType.Values.TryGetValue(coordinate.MemberName, out var enumValue))
                    {
                        member = enumValue;
                        return true;
                    }
                }

                if (type.Kind is TypeKind.InputObject)
                {
                    var inputType = (InputObjectTypeDefinition)type;
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

            var complexType = (ComplexTypeDefinition)type;
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

    public static SchemaDefinition Create(string name) => new() { Name = name, };
}
