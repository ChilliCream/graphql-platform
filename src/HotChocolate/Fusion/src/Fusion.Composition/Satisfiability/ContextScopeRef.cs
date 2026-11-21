namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// A reference to a context scope interned by <see cref="ContextScopes"/>, only meaningful together
/// with the instance that created it.
/// </summary>
/// <param name="Id">The identifier of the referenced scope within its <see cref="ContextScopes"/>.</param>
internal readonly record struct ContextScopeRef(int Id)
{
    /// <summary>
    /// Gets the reference to the scope without entries.
    /// </summary>
    public static ContextScopeRef Empty => default;
}
