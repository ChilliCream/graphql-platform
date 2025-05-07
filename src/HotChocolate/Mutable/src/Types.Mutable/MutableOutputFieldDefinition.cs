using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL output field definition.
/// </summary>
public class MutableOutputFieldDefinition
    : INamedTypeSystemMemberDefinition<MutableOutputFieldDefinition>
    , IOutputFieldDefinition
    , IMutableFieldDefinition
    , IFeatureProvider
{
    private bool _isDeprecated;
    private string? _deprecationReason;
    private DirectiveCollection? _directives;
    private InputFieldDefinitionCollection? _arguments;
    private IOutputType _type;

    /// <summary>
    /// Initializes a new instance of the <see cref="MutableOutputFieldDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="type">The type of the field.</param>
    public MutableOutputFieldDefinition(string name, IOutputType? type = null)
    {
        Name = name;
        _type = type ?? NotSetType.Default;
    }

    /// <inheritdoc cref="IMutableFieldDefinition.Name" />
    public string Name
    {
        get => field;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IMutableFieldDefinition.Description" />
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the declaring member of the field.
    /// </summary>
    public IComplexTypeDefinition? DeclaringMember { get; set; }

    IComplexTypeDefinition IOutputFieldDefinition.DeclaringType
        => DeclaringMember ?? throw new InvalidOperationException("The declaring type is not set.");

    ITypeSystemMember IFieldDefinition.DeclaringMember
        => DeclaringMember ?? throw new InvalidOperationException("The declaring type is not set.");

    public SchemaCoordinate Coordinate
    {
        get
        {
            if (DeclaringMember is null)
            {
                throw new InvalidOperationException("The declaring type is not set.");
            }

            return new SchemaCoordinate(DeclaringMember.Name, Name, ofDirective: false);
        }
    }

    /// <inheritdoc cref="IMutableFieldDefinition.IsDeprecated" />
    public bool IsDeprecated
    {
        get => _isDeprecated;
        set
        {
            _isDeprecated = value;

            if (!value)
            {
                _deprecationReason = null;
            }
        }
    }

    /// <inheritdoc cref="IMutableFieldDefinition.DeprecationReason" />
    public string? DeprecationReason
    {
        get => _deprecationReason;
        set
        {
            _deprecationReason = value;

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

    /// <summary>
    /// Gets the arguments that are accepted by this field.
    /// </summary>
    public InputFieldDefinitionCollection Arguments
        => _arguments ??= new InputFieldDefinitionCollection(this);

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IOutputFieldDefinition.Arguments
        => _arguments ?? EmptyCollections.InputFieldDefinitions;

    /// <summary>
    /// Gets the type of the field.
    /// </summary>
    /// <value>
    /// The type of the field.
    /// </value>
    public IOutputType Type
    {
        get => _type;
        set => _type = value.ExpectOutputType();
    }

    IType IMutableFieldDefinition.Type
    {
        get => Type;
        set => Type = value.ExpectOutputType();
    }

    IType IFieldDefinition.Type => Type;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="FieldDefinitionNode"/> from an <see cref="MutableOutputFieldDefinition"/>.
    /// </summary>
    public FieldDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => Format(this);

    /// <summary>
    /// Creates a new output field definition.
    /// </summary>
    /// <param name="name">
    /// The name of the output field definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="MutableOutputFieldDefinition"/>.
    /// </returns>
    public static MutableOutputFieldDefinition Create(string name) => new(name);
}
