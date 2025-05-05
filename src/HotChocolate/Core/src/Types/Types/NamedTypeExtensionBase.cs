using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// This is not a full type and is used to split the type configuration into multiple part.
/// Any type extension instance will not survive the initialization and instead is
/// merged into the target type.
/// </summary>
public abstract class NamedTypeExtensionBase<TConfiguration>
    : TypeSystemObject<TConfiguration>
    , ITypeDefinition
    , ITypeDefinitionExtensionMerger
    where TConfiguration : TypeSystemConfiguration, ITypeConfiguration
{
    /// <inheritdoc />
    public abstract TypeKind Kind { get; }

    /// <summary>
    /// Gets the type extended by this type extension.
    /// </summary>
    public Type? ExtendsType { get; private set; }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => throw new NotSupportedException();

    protected abstract void Merge(
        ITypeCompletionContext context,
        ITypeDefinition type);

    void ITypeDefinitionExtensionMerger.Merge(
        ITypeCompletionContext context,
        ITypeDefinition type)
        => Merge(context, type);

    protected override void OnAfterCompleteName(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
        ExtendsType = Configuration?.ExtendsType;
        base.OnAfterCompleteName(context, configuration);
    }

    public bool Equals(IType? other)
        => ReferenceEquals(this, other);

    public bool IsAssignableFrom(ITypeDefinition type) => throw new NotSupportedException();

    public ISyntaxNode ToSyntaxNode() => throw new NotSupportedException();
}

/// <summary>
/// This internal interface is used by the type initialization to
/// merge the type extension into the actual type.
/// </summary>
internal interface ITypeDefinitionExtensionMerger
{
    /// <summary>
    /// The merge method that allows to merge the type extension into the named type.
    /// </summary>
    /// <param name="context">The type extension completion context.</param>
    /// <param name="type">The target type into which we merge the type extension.</param>
    void Merge(ITypeCompletionContext context, ITypeDefinition type);
}
