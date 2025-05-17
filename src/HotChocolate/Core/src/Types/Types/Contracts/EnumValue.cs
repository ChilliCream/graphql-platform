#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL enum value.
/// </summary>
public abstract class EnumValue
    : IHasDirectives
    , IHasReadOnlyContextData
    , ITypeSystemMember
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
    public abstract IDirectiveCollection Directives { get; }

    /// <summary>
    /// Gets the context data dictionary that can be used by middleware components and
    /// resolvers to retrieve data during execution.
    /// </summary>
    public abstract IReadOnlyDictionary<string, object?> ContextData { get; }

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

    void IEnumValueCompletion.CompleteMetadata(ITypeCompletionContext context, ITypeSystemMember declaringMember)
        => OnCompleteMetadata(context, declaringMember);
}
