using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types.Helpers;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL enum value.
/// </summary>
public abstract class EnumValue
    : IEnumValue
    , IEnumValueCompletion
{
    /// <summary>
    /// The GraphQL name of this enum value.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the GraphQL description for this enum value.
    /// </summary>
    public abstract string? Description { get; }

    /// <summary>
    /// Gets the enum type that declares this value.
    /// </summary>
    public EnumType DeclaringType { get; private set; } = null!;

    IEnumTypeDefinition IEnumValue.DeclaringType => DeclaringType;

    /// <summary>
    /// Gets the coordinate of this enum value.
    /// </summary>
    public SchemaCoordinate Coordinate => new(DeclaringType.Name, Name, ofDirective: false);

    /// <summary>
    /// Defines if this enum value is deprecated.
    /// </summary>
    public abstract bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason for this enum value.
    /// </summary>
    public abstract string? DeprecationReason { get; }

    /// <summary>
    /// Gets the runtime value.
    /// </summary>
    public abstract object Value { get; }

    /// <summary>
    /// Gets the directives of this enum value.
    /// </summary>
    public abstract DirectiveCollection Directives { get; }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives.AsReadOnlyDirectiveCollection();

    /// <summary>
    /// Gets the features of this enum value.
    /// </summary>
    public abstract IFeatureCollection Features { get; }

    /// <summary>
    /// Will be invoked before the metadata of this enum value is completed.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The declaring member of this enum value.
    /// </param>
    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
    }

    private void CompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        DeclaringType = (EnumType)declaringMember;
        OnCompleteMetadata(context, declaringMember);
    }

    void IEnumValueCompletion.CompleteMetadata(ITypeCompletionContext context, ITypeSystemMember declaringMember)
        => CompleteMetadata(context, declaringMember);

    /// <summary>
    /// Creates a <see cref="EnumValueDefinitionNode"/> that represents the enum value.
    /// </summary>
    /// <returns>
    /// The GraphQL syntax node that represents the enum value.
    /// </returns>
    public EnumValueDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => ToSyntaxNode();
}
