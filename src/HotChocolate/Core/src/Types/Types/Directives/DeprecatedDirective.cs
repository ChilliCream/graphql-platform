using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The @deprecated directive is used within the type system definition
/// language to indicate deprecated portions of a GraphQL service’s schema,
/// such as deprecated fields on a type or deprecated enum values.
///
/// Deprecations include a reason for why it is deprecated,
/// which is formatted using Markdown syntax (as specified by CommonMark).
/// </summary>
[Serializable]
public sealed class DeprecatedDirective
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeprecatedDirective"/>
    /// </summary>
    public DeprecatedDirective()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DeprecatedDirective"/>
    /// </summary>
    /// <param name="reason">
    /// The deprecation reason.
    /// </param>
    public DeprecatedDirective(string? reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the deprecation reason.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Returns a deprecation directive syntax node representation.
    /// </summary>
    /// <returns></returns>
    public DirectiveNode ToNode() => CreateNode(Reason);

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString() => ToNode().ToString();

    /// <summary>
    /// Creates a deprecation directive syntax node.
    /// </summary>
    /// <param name="reason">
    /// The deprecation reason.
    /// </param>
    /// <returns>
    /// Returns a new deprecation directive syntax node.
    /// </returns>
    public static DirectiveNode CreateNode(string? reason = null)
    {
        if (WellKnownDirectives.DeprecationDefaultReason.EqualsOrdinal(reason))
        {
            reason = null;
        }

        var arguments = reason is null
            ? Array.Empty<ArgumentNode>()
            : [new ArgumentNode(WellKnownDirectives.DeprecationReasonArgument, reason),];

        return new DirectiveNode(
            null,
            new NameNode(WellKnownDirectives.Deprecated),
            arguments);
    }
}
