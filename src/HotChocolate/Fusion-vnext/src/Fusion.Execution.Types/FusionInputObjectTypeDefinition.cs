using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using static HotChocolate.Fusion.Types.ThrowHelper;

namespace HotChocolate.Fusion.Types;

public sealed class FusionInputObjectTypeDefinition : IInputObjectTypeDefinition
{
    private bool _completed;

    public FusionInputObjectTypeDefinition(
        string name,
        string? description,
        FusionInputFieldDefinitionCollection fields)
    {
        Name = name;
        Description = description;
        Fields = fields;

        // these properties are initialized
        // in the type complete step.
        Directives = null!;
        Features = null!;
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.InputObject;

    /// <inheritdoc />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    public string Name { get; }

    public string? Description { get; }

    public FusionInputFieldDefinitionCollection Fields { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IInputObjectTypeDefinition.Fields => Fields;

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

        Directives = context.Directives;
        Features = context.Features;

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
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.Kind == TypeKind.InputObject)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }
}
