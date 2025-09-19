using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL directive definition.
/// </summary>
public class MutableDirectiveDefinition
    : INamedTypeSystemMemberDefinition<MutableDirectiveDefinition>
    , IDirectiveDefinition
    , IFeatureProvider
{
    private InputFieldDefinitionCollection? _arguments;

    /// <summary>
    /// Represents a GraphQL directive definition.
    /// </summary>
    public MutableDirectiveDefinition(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets or sets the name of the directive.
    /// </summary>
    /// <value>
    /// The name of the directive.
    /// </value>
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <summary>
    /// Gets or sets the description of the directive.
    /// </summary>
    /// <value>
    /// The description of the directive.
    /// </value>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this directive type is a spec directive.
    /// </summary>
    public bool IsSpecDirective { get; set; }

    /// <summary>
    /// Defines if this directive is repeatable and can be applied multiple times.
    /// </summary>
    public bool IsRepeatable { get; set; }

    /// <summary>
    /// Gets the arguments that are defined on this directive.
    /// </summary>
    public InputFieldDefinitionCollection Arguments
        => _arguments ??= new(this);

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IDirectiveDefinition.Arguments
        => _arguments ?? EmptyCollections.InputFieldDefinitions;

    /// <summary>
    /// Gets the locations where this directive can be applied.
    /// </summary>
    /// <value>
    /// The locations where this directive can be applied.
    /// </value>
    public DirectiveLocation Locations { get; set; }

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

    public SchemaCoordinate Coordinate
        => new(Name, ofDirective: true);

    public Type RuntimeType => typeof(object);

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="DirectiveDefinitionNode"/>
    /// from a <see cref="MutableDirectiveDefinition"/>.
    /// </summary>
    public DirectiveDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => Format(this);

    /// <summary>
    /// Creates a new directive definition.
    /// </summary>
    /// <param name="name">
    /// The name of the directive.
    /// </param>
    /// <returns>
    /// Returns a new directive definition.
    /// </returns>
    public static MutableDirectiveDefinition Create(string name)
        => new(name);
}
