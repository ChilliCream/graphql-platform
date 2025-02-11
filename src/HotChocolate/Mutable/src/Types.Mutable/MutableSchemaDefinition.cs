using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL schema definition.
/// </summary>
public class MutableSchemaDefinition
    : INamedTypeSystemMemberDefinition<MutableSchemaDefinition>
    , ISchemaDefinition
    , IFeatureProvider
{
    private readonly List<SchemaCoordinate> _allDefinitionCoordinates = [];
    private MutableObjectTypeDefinition? _queryType;
    private MutableObjectTypeDefinition? _mutationType;
    private MutableObjectTypeDefinition? _subscriptionType;
    private TypeDefinitionCollection? _typeDefinitions;
    private DirectiveDefinitionCollection? _directiveDefinitions;
    private DirectiveCollection? _directives;
    private IFeatureCollection? _features;

    /// <inheritdoc />
    public string Name { get; set; } = "default";

    /// <inheritdoc cref="IDescriptionProvider.Description" />
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the query type.
    /// </summary>
    public MutableObjectTypeDefinition? QueryType
    {
        get => _queryType;
        set
        {
            _queryType = value;

            if (value is not null && !Types.Contains(value))
            {
                Types.Add(value);
            }
        }
    }

    IObjectTypeDefinition? ISchemaDefinition.QueryType => QueryType;

    /// <summary>
    /// Gets or sets the mutation type.
    /// </summary>
    public MutableObjectTypeDefinition? MutationType
    {
        get => _mutationType;
        set
        {
            _mutationType = value;

            if (value is not null && !Types.Contains(value))
            {
                Types.Add(value);
            }
        }
    }

    IObjectTypeDefinition? ISchemaDefinition.MutationType => MutationType;

    /// <summary>
    /// Gets or sets the subscription type.
    /// </summary>
    public MutableObjectTypeDefinition? SubscriptionType
    {
        get => _subscriptionType;
        set
        {
            _subscriptionType = value;

            if (value is not null && !Types.Contains(value))
            {
                Types.Add(value);
            }
        }
    }

    IObjectTypeDefinition? ISchemaDefinition.SubscriptionType => SubscriptionType;

    /// <summary>
    /// Gets the types that are defined in this schema.
    /// </summary>
    public TypeDefinitionCollection Types
        => _typeDefinitions ??= new TypeDefinitionCollection(_allDefinitionCoordinates);

    IReadOnlyTypeDefinitionCollection ISchemaDefinition.Types => Types;

    /// <summary>
    /// Gets the directives that are defined in this schema.
    /// </summary>
    public DirectiveDefinitionCollection DirectiveDefinitions
        => _directiveDefinitions ??= new DirectiveDefinitionCollection(_allDefinitionCoordinates);

    IReadOnlyDirectiveDefinitionCollection ISchemaDefinition.DirectiveDefinitions => DirectiveDefinitions;

    /// <summary>
    /// Gets the directives that are annotated to this schema.
    /// </summary>
    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection ISchemaDefinition.Directives => Directives;

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

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
                    var enumType = (MutableEnumTypeDefinition)type;
                    if (enumType.Values.TryGetValue(coordinate.MemberName, out var enumValue))
                    {
                        member = enumValue;
                        return true;
                    }
                }

                if (type.Kind is TypeKind.InputObject)
                {
                    var inputType = (MutableInputObjectTypeDefinition)type;
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

            var complexType = (MutableComplexTypeDefinition)type;
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

    public MutableObjectTypeDefinition GetOperationType(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Query => QueryType ?? throw new InvalidOperationException(),
            OperationType.Mutation => MutationType ?? throw new InvalidOperationException(),
            OperationType.Subscription => SubscriptionType ?? throw new InvalidOperationException(),
            _ => throw new NotSupportedException()
        };
    }

    IObjectTypeDefinition ISchemaDefinition.GetOperationType(OperationType operationType)
        => GetOperationType(operationType);

    /// <summary>
    /// Gets the type and directive definitions that are defined in this schema in insert order.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<INameProvider> GetAllDefinitions()
    {
        foreach (var definition in _allDefinitionCoordinates)
        {
            yield return definition.OfDirective
                ? DirectiveDefinitions[definition.Name]
                : Types[definition.Name];
        }
    }

    /// <summary>
    /// Returns a string representation of the schema.
    /// </summary>
    /// <returns>
    /// A string representation of the schema.
    /// </returns>
    public override string ToString()
        => SchemaFormatter.FormatAsString(this);

    /// <summary>
    /// Returns a string representation of the schema.
    /// </summary>
    /// <param name="options">
    /// The options that control the formatting of the schema document.
    /// </param>
    /// <returns>
    /// A string representation of the schema.
    /// </returns>
    public string ToString(SchemaFormatterOptions options)
        => SchemaFormatter.FormatAsString(this, options);

    /// <summary>
    /// Returns a syntax node representation of the schema.
    /// </summary>
    /// <returns>
    /// A syntax node representation of the schema.
    /// </returns>
    public DocumentNode ToSyntaxNode()
        => ToSyntaxNode(default);

    /// <summary>
    /// Returns a syntax node representation of the schema.
    /// </summary>
    /// <param name="options">
    /// The options that control the formatting of the schema document.
    /// </param>
    /// <returns>
    /// A syntax node representation of the schema.
    /// </returns>
    public DocumentNode ToSyntaxNode(SchemaFormatterOptions options)
        => SchemaFormatter.FormatAsDocument(this, options);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => ToSyntaxNode();

    /// <summary>
    /// Creates a new schema definition.
    /// </summary>
    /// <param name="name">
    /// The name of the schema.
    /// </param>
    /// <returns>
    /// Returns a new schema definition.
    /// </returns>
    public static MutableSchemaDefinition Create(string name) => new() { Name = name, };
}
