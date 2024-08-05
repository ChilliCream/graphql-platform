using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// This is not a full type and is used to split the type configuration into multiple part.
/// Any type extension instance is will not survive the initialization and instead is
/// merged into the target type.
/// </summary>
public abstract class NamedTypeExtensionBase<TDefinition>
    : TypeSystemObjectBase<TDefinition>
    , INamedTypeExtensionMerger
    where TDefinition : DefinitionBase, ITypeDefinition
{
    /// <inheritdoc />
    public abstract TypeKind Kind { get; }

    /// <inheritdoc />
    public Type? ExtendsType { get; private set; }

    protected abstract void Merge(
        ITypeCompletionContext context,
        INamedType type);

    void INamedTypeExtensionMerger.Merge(
        ITypeCompletionContext context,
        INamedType type) => Merge(context, type);

    protected override void OnAfterCompleteName(
        ITypeCompletionContext context,
        DefinitionBase definition)
    {
        ExtendsType = Definition?.ExtendsType;
        base.OnAfterCompleteName(context, definition);
    }
}
