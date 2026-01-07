using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Types.ThrowHelper;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL input object type definition in a fusion schema.
/// </summary>
public sealed class FusionInputObjectTypeDefinition : IInputObjectTypeDefinition, IFusionTypeDefinition
{
    private InputObjectFlags _flags;
    private bool _completed;

    /// <summary>
    /// Initializes a new instance of <see cref="FusionInputObjectTypeDefinition"/>.
    /// </summary>
    /// <param name="name">The name of the input object type.</param>
    /// <param name="description">The description of the input object type.</param>
    /// <param name="isInaccessible">A value indicating whether the input object type is marked as inaccessible.</param>
    /// <param name="fields">The collection of input fields.</param>
    public FusionInputObjectTypeDefinition(
        string name,
        string? description,
        bool isInaccessible,
        FusionInputFieldDefinitionCollection fields)
    {
        name.EnsureGraphQLName();
        ArgumentNullException.ThrowIfNull(fields);

        Name = name;
        Description = description;
        Fields = fields;

        if (isInaccessible)
        {
            _flags = InputObjectFlags.Inaccessible;
        }

        // these properties are initialized
        // in the type complete step.
        Directives = null!;
        Features = null!;
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.InputObject;

    /// <inheritdoc />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    /// <summary>
    /// Gets the name of this input object type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this input object type.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the collection of input fields for this input object type.
    /// </summary>
    public FusionInputFieldDefinitionCollection Fields { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IInputObjectTypeDefinition.Fields => Fields;

    /// <inheritdoc />
    public bool IsOneOf => (InputObjectFlags.OneOf & _flags) == InputObjectFlags.OneOf;

    /// <summary>
    /// Gets a value indicating whether this input object type is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible => (InputObjectFlags.Inaccessible & _flags) == InputObjectFlags.Inaccessible;

    /// <summary>
    /// Gets the directives applied to this input object type.
    /// </summary>
    public FusionDirectiveCollection Directives
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => Directives;

    /// <summary>
    /// Gets the feature collection associated with this input object type.
    /// </summary>
    public IFeatureCollection Features
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeInputObjectTypeCompletionContext context)
    {
        EnsureNotSealed(_completed);

        if (context.Directives is null || context.Features is null)
        {
            throw InvalidCompletionContext();
        }

        Directives = context.Directives;
        Features = context.Features;

        if (context.Directives.ContainsName(WellKnownDirectiveNames.OneOf))
        {
            _flags |= InputObjectFlags.OneOf;
        }

        _completed = true;
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="InputObjectTypeDefinitionNode"/>
    /// from a <see cref="FusionInputObjectTypeDefinition"/>.
    /// </summary>
    public InputObjectTypeDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    /// <inheritdoc />
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is FusionInputObjectTypeDefinition otherInputObject
            && otherInputObject.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        if (type.Kind == TypeKind.InputObject)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }

    [Flags]
    private enum InputObjectFlags
    {
        None = 0,
        OneOf = 2,
        Inaccessible = 4
    }
}
