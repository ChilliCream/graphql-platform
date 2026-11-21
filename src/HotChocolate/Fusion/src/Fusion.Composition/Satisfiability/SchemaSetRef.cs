namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// A reference to a set of source schemas interned by a <see cref="SchemaSetInterner"/>, only
/// meaningful together with the interner that created it.
/// </summary>
/// <param name="Id">The identifier of the referenced set within its interner.</param>
internal readonly record struct SchemaSetRef(int Id)
{
    /// <summary>
    /// Gets the reference to the empty set.
    /// </summary>
    public static SchemaSetRef Empty => default;
}
