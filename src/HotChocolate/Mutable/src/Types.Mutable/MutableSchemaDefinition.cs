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

    /// <inheritdoc />
    public string Name { get; set; } = "default";

    /// <inheritdoc cref="IDescriptionProvider.Description" />
    public string? Description { get; set; }

    /// <inheritdoc cref="ISchemaDefinition.Services" />
    public IServiceProvider Services => EmptyServiceProvider.Instance;

    /// <summary>
    /// Gets or sets the query type.
    /// </summary>
    public MutableObjectTypeDefinition? QueryType
    {
        get;
        set
        {
            field = value;

            if (value is not null && !Types.Contains(value))
            {
                Types.Add(value);
            }
        }
    }

    IObjectTypeDefinition ISchemaDefinition.QueryType
        => QueryType ?? throw new InvalidOperationException("The query type is not defined.");

    /// <summary>
    /// Gets or sets the mutation type.
    /// </summary>
    public MutableObjectTypeDefinition? MutationType
    {
        get;
        set
        {
            field = value;

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
        get;
        set
        {
            field = value;

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
    [field: AllowNull, MaybeNull]
    public TypeDefinitionCollection Types
        => field ??= new TypeDefinitionCollection(_allDefinitionCoordinates);

    IReadOnlyTypeDefinitionCollection ISchemaDefinition.Types => Types;

    /// <summary>
    /// Gets the directives that are defined in this schema.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public DirectiveDefinitionCollection DirectiveDefinitions
        => field ??= new DirectiveDefinitionCollection(_allDefinitionCoordinates);

    IReadOnlyDirectiveDefinitionCollection ISchemaDefinition.DirectiveDefinitions => DirectiveDefinitions;

    /// <summary>
    /// Gets the directives that are annotated to this schema.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public DirectiveCollection Directives
        => field ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

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

    public MutableObjectTypeDefinition GetOperationType(OperationType operation)
    {
        return operation switch
        {
            OperationType.Query => QueryType ?? throw new InvalidOperationException(),
            OperationType.Mutation => MutationType ?? throw new InvalidOperationException(),
            OperationType.Subscription => SubscriptionType ?? throw new InvalidOperationException(),
            _ => throw new NotSupportedException()
        };
    }

    IObjectTypeDefinition ISchemaDefinition.GetOperationType(OperationType operation)
        => GetOperationType(operation);

    public bool TryGetOperationType(
        OperationType operation,
        [NotNullWhen(true)] out MutableObjectTypeDefinition? type)
    {
        type = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => throw new NotSupportedException()
        };
        return type is not null;
    }

    bool ISchemaDefinition.TryGetOperationType(
        OperationType operation,
        [NotNullWhen(true)] out IObjectTypeDefinition? type)
    {
        type = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => throw new NotSupportedException()
        };
        return type is not null;
    }

    public IEnumerable<MutableObjectTypeDefinition> GetPossibleTypes(ITypeDefinition abstractType)
    {
        if (abstractType.Kind is not TypeKind.Union and not TypeKind.Interface and not TypeKind.Object)
        {
            throw new ArgumentException(
                "The specified type is not an abstract type.",
                nameof(abstractType));
        }

        if (abstractType is MutableUnionTypeDefinition unionType)
        {
            foreach (var possibleType in unionType.Types.AsEnumerable())
            {
                yield return possibleType;
            }

            yield break;
        }

        if (abstractType is MutableInterfaceTypeDefinition interfaceType)
        {
            foreach (var type in Types)
            {
                if (type is MutableObjectTypeDefinition obj
                    && obj.Implements.ContainsName(interfaceType.Name))
                {
                    yield return obj;
                }
            }

            yield break;
        }

        if (abstractType is MutableObjectTypeDefinition objectType)
        {
            yield return objectType;
        }
    }

    IEnumerable<IObjectTypeDefinition> ISchemaDefinition.GetPossibleTypes(ITypeDefinition abstractType)
        => GetPossibleTypes(abstractType);

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
    public static MutableSchemaDefinition Create(string name) => new() { Name = name };

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;

        public static EmptyServiceProvider Instance { get; } = new();
    }
}
