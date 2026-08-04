namespace Mocha.Analyzers;

/// <summary>
/// Equatable per-type metadata captured in a pipeline keyed on the type's own declaration.
/// Editing the file that declares the type invalidates exactly this entry, so message and
/// response documentation and locations no longer go stale when only the message file changes.
/// </summary>
/// <param name="TypeName">
/// The fully qualified name of the declared type, used as the join key against handler and
/// context message type names. For a declaration this is the original definition
/// (for example <c>global::Ns.Wrapper&lt;T&gt;</c> for a generic type).
/// </param>
/// <param name="XmlDocumentation">
/// The XML documentation captured from the declaring symbol, or <see langword="null"/> if none.
/// </param>
/// <param name="Location">
/// The full source location of the type declaration, or <see langword="null"/> if unavailable.
/// </param>
public sealed record DeclaredTypeInfo(
    string TypeName,
    string? XmlDocumentation,
    LocationInfo? Location);
