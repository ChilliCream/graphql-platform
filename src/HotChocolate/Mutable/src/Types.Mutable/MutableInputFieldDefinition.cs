using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL input field definition.
/// </summary>
public class MutableInputFieldDefinition
    : INamedTypeSystemMemberDefinition<MutableInputFieldDefinition>
    , IInputValueDefinition
    , IMutableFieldDefinition
{
    private IInputType _type;
    private bool _isDeprecated;
    private DirectiveCollection? _directives;

    /// <summary>
    /// Represents a GraphQL input field definition.
    /// </summary>
    public MutableInputFieldDefinition(string name, IInputType? type = null)
    {
        Name = name;
        _type = type ?? NotSetType.Default;
    }

    /// <inheritdoc cref="IMutableFieldDefinition.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IMutableFieldDefinition.Description" />
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the declaring member of the input field.
    /// </summary>
    public ITypeSystemMember? DeclaringMember
    {
        get;
        set
        {
            if (value is not MutableInputObjectTypeDefinition
                and not MutableDirectiveDefinition
                and not MutableOutputFieldDefinition
                and not null)
            {
                throw new ArgumentException(
                    "The declaring member must be an input object type, a directive or an output field.",
                    nameof(value));
            }

            field = value;
        }
    }

    ITypeSystemMember IFieldDefinition.DeclaringMember
        => DeclaringMember ?? throw new InvalidOperationException("The declaring member is not set.");

    /// <inheritdoc />
    public SchemaCoordinate Coordinate
    {
        get
        {
            switch (DeclaringMember)
            {
                case IInputObjectTypeDefinition typeDef:
                    return new SchemaCoordinate(typeDef.Name, Name, ofDirective: false);

                case IDirectiveDefinition directiveDef:
                    return new SchemaCoordinate(directiveDef.Name, Name, ofDirective: true);

                case IOutputFieldDefinition fieldDef:
                    if (fieldDef.DeclaringType is null)
                    {
                        throw new InvalidOperationException("The declaring member is not set.");
                    }

                    return new SchemaCoordinate(
                        fieldDef.DeclaringType.Name,
                        fieldDef.Name,
                        Name,
                        ofDirective: false);

                default:
                    throw new InvalidOperationException("The declaring type is not set.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the default value for this input field.
    /// </summary>
    /// <value></value>
    public IValueNode? DefaultValue { get; set; }

    /// <inheritdoc cref="IMutableFieldDefinition.IsDeprecated" />
    public bool IsDeprecated
    {
        get => _isDeprecated;
        set
        {
            _isDeprecated = value;

            if (!value)
            {
                DeprecationReason = null;
            }
        }
    }

    /// <inheritdoc cref="IMutableFieldDefinition.DeprecationReason" />
    public string? DeprecationReason
    {
        get;
        set
        {
            field = value;

            if (!string.IsNullOrEmpty(value))
            {
                _isDeprecated = true;
            }
        }
    }

    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives ?? EmptyCollections.Directives;

    /// <inheritdoc cref="IMutableFieldDefinition.Type" />
    public IInputType Type
    {
        get => _type;
        set => _type = value.ExpectInputType();
    }

    public FieldFlags Flags { get; set; }

    IType IMutableFieldDefinition.Type
    {
        get => Type;
        set => Type = value.ExpectInputType();
    }

    IType IFieldDefinition.Type => _type;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => Format(this).ToString(true);

    /// <summary>
    /// Creates an <see cref="InputValueDefinitionNode"/> from an
    /// <see cref="MutableInputFieldDefinition"/>.
    /// </summary>
    public InputValueDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => Format(this);

    /// <summary>
    /// Creates a new instance of <see cref="MutableInputFieldDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the input field.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="MutableInputFieldDefinition"/>.
    /// </returns>
    public static MutableInputFieldDefinition Create(string name) => new(name);
}
