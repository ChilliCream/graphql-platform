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
    , ISealable
{
    private ObjectTypeDefinition? _queryType;
    private ObjectTypeDefinition? _mutationType;
    private ObjectTypeDefinition? _subscriptionType;
    private ITypeDefinitionCollection? _typeDefinitions;
    private IDirectiveDefinitionCollection? _directiveDefinitions;
    private IDirectiveCollection? _directives;
    private IFeatureCollection? _features;
    private bool _isReadOnly;

    /// <inheritdoc />
    public string Name { get; set; } = "default";

    /// <inheritdoc cref="IDescriptionProvider.Description" />
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the query type.
    /// </summary>
    public ObjectTypeDefinition? QueryType
    {
        get => _queryType;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The type is sealed and cannot be modified.");
            }

            _queryType = value;

            if (value is not null && !Types.Contains(value))
            {
                Types.Add(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the mutation type.
    /// </summary>
    public ObjectTypeDefinition? MutationType
    {
        get => _mutationType;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The type is sealed and cannot be modified.");
            }

            _mutationType = value;

            if (value is not null && !Types.Contains(value))
            {
                Types.Add(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the subscription type.
    /// </summary>
    public ObjectTypeDefinition? SubscriptionType
    {
        get => _subscriptionType;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The type is sealed and cannot be modified.");
            }

            _subscriptionType = value;

            if (value is not null && !Types.Contains(value))
            {
                Types.Add(value);
            }
        }
    }

    /// <summary>
    /// Gets the types that are defined in this schema.
    /// </summary>
    public ITypeDefinitionCollection Types
        => _typeDefinitions ??= new TypeDefinitionCollection();

    /// <summary>
    /// Gets the directives that are defined in this schema.
    /// </summary>
    public IDirectiveDefinitionCollection DirectiveDefinitions
        => _directiveDefinitions ??= new DirectiveDefinitionCollection();

    /// <summary>
    /// Gets the directives that are annotated to this schema.
    /// </summary>
    public IDirectiveCollection Directives
        => _directives ??= new DirectiveCollection();

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Seals this type and makes it read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    internal void Seal()
    {
        if (_isReadOnly)
        {
            return;
        }

        if(_typeDefinitions is null || _typeDefinitions.Count == 0)
        {
            throw new InvalidOperationException(
                "A schema must have at least one type.");
        }

        _typeDefinitions = _typeDefinitions is null
            ? ReadOnlyTypeDefinitionCollection.Empty
            : ReadOnlyTypeDefinitionCollection.From(_typeDefinitions);

        _directiveDefinitions = _directiveDefinitions is null
            ? ReadOnlyDirectiveDefinitionCollection.Empty
            : ReadOnlyDirectiveDefinitionCollection.From(_directiveDefinitions);

        _directives = _directives is null
            ? ReadOnlyDirectiveCollection.Empty
            : ReadOnlyDirectiveCollection.From(_directives);

        _features = _features is null
            ? EmptyFeatureCollection.Default
            : _features.ToReadOnly();

        foreach (var typeDefinition in _typeDefinitions)
        {
            ((ISealable)typeDefinition).Seal();
        }

        foreach (var directiveDefinition in _directiveDefinitions)
        {
            ((ISealable)directiveDefinition).Seal();
        }

        _isReadOnly = true;
    }

    void ISealable.Seal() => Seal();

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
