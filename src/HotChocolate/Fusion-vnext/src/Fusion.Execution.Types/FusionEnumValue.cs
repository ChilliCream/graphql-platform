using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a value of a GraphQL enum type in a fusion schema.
/// </summary>
public sealed class FusionEnumValue : IEnumValue, IInaccessibleProvider
{
    private bool _completed;

    /// <summary>
    /// Initializes a new instance of <see cref="FusionEnumValue"/>.
    /// </summary>
    /// <param name="name">The name of the enum value.</param>
    /// <param name="description">The description of the enum value.</param>
    /// <param name="isDeprecated">A value indicating whether the enum value is deprecated.</param>
    /// <param name="deprecationReason">The deprecation reason if the enum value is deprecated.</param>
    /// <param name="isInaccessible">A value indicating whether the enum value is marked as inaccessible.</param>
    public FusionEnumValue(
        string name,
        string? description,
        bool isDeprecated,
        string? deprecationReason,
        bool isInaccessible)
    {
        name.EnsureGraphQLName();

        Name = name;
        Description = description;
        IsDeprecated = isDeprecated;
        DeprecationReason = deprecationReason;
        IsInaccessible = isInaccessible;

        // these properties are initialized
        // in the type complete step.
        DeclaringType = null!;
        Directives = null!;
        Features = null!;
    }

    /// <summary>
    /// Gets the name of this enum value.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this enum value.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the enum type that declares this value.
    /// </summary>
    public IEnumTypeDefinition DeclaringType
    {
        get;
        set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    /// <summary>
    /// Gets the schema coordinate of this enum value.
    /// </summary>
    public SchemaCoordinate Coordinate => new(DeclaringType.Name, Name, ofDirective: false);

    /// <summary>
    /// Gets a value indicating whether this enum value is deprecated.
    /// </summary>
    public bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason if the enum value is deprecated.
    /// </summary>
    public string? DeprecationReason { get; }

    /// <summary>
    /// Gets a value indicating whether this enum value is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible { get; }

    /// <summary>
    /// Gets the directives applied to this enum value.
    /// </summary>
    public FusionDirectiveCollection Directives
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    /// <summary>
    /// Gets the feature collection associated with this enum value.
    /// </summary>
    public IFeatureCollection Features
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    /// <summary>
    /// Completes the initialization of this enum value by setting properties
    /// that are populated during the schema completion phase.
    /// </summary>
    /// <param name="context">The completion context containing required properties.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the context has an invalid state (required properties are null).
    /// </exception>
    internal void Complete(CompositeEnumValueCompletionContext context)
    {
        ThrowHelper.EnsureNotSealed(_completed);

        if (context.DeclaringType is null || context.Directives is null || context.Features is null)
        {
            throw ThrowHelper.InvalidCompletionContext();
        }

        DeclaringType = context.DeclaringType;
        Directives = context.Directives;
        Features = context.Features;
        _completed = true;
    }

    /// <summary>
    /// Gets the string representation of this enum value.
    /// </summary>
    /// <returns>
    /// The string representation of this enum value.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(indented: true);

    /// <summary>
    /// Creates an <see cref="EnumValueDefinitionNode"/> from this
    /// <see cref="FusionEnumValue"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="EnumValueDefinitionNode"/> representing this enum value.
    /// </returns>
    public EnumValueDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);
}
