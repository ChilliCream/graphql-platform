using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Carries the aggregated per-type declaration metadata through the shared
/// <c>ImmutableArray&lt;SyntaxInfo&gt;</c> pipeline so the dependency injection generator can
/// resolve message and response documentation and spans by fully qualified type name.
/// </summary>
/// <param name="Declarations">
/// The declaration metadata for every message type declared in this compilation and observed by any
/// discovery source, sorted by <see cref="DeclaredTypeInfo.TypeName"/> and deduplicated so the carrier
/// has stable value equality.
/// </param>
/// <param name="ExplicitAddMessageTypeNames">
/// The fully qualified names of message types the user registers with an explicit <c>AddMessage&lt;T&gt;()</c>
/// call. The generated module emits only the descriptor callback for these, never a second
/// <c>AddMessage</c>, because the user's own call already performs the registration.
/// </param>
public sealed record MessageDeclarationsInfo(
    ImmutableEquatableArray<DeclaredTypeInfo> Declarations,
    ImmutableEquatableArray<string> ExplicitAddMessageTypeNames) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => "MsgDeclarations";
}
