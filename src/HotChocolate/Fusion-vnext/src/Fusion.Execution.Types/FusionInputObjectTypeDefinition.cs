using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionInputObjectTypeDefinition(
    string name,
    string? description,
    FusionInputFieldDefinitionCollection fields)
    : IInputObjectTypeDefinition
{
    private FusionDirectiveCollection _directives = default!;
    private bool _completed;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.InputObject;

    /// <inheritdoc />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    public string Name => name;

    public string? Description => description;

    public FusionDirectiveCollection Directives => _directives;

    public FusionInputFieldDefinitionCollection Fields => fields;

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IInputObjectTypeDefinition.Fields => Fields;

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    internal void Complete(CompositeInputObjectTypeCompletionContext context)
    {
        ThrowHelper.EnsureNotSealed(_completed);

        _directives = context.Directives;

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
