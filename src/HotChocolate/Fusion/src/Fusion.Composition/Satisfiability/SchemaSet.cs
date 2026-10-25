namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// An interned set of source schemas that is only meaningful together with the
/// <see cref="SchemaSetTable"/> that created it.
/// </summary>
/// <param name="Id">The identifier of the set within its table.</param>
internal readonly record struct SchemaSet(int Id)
{
    /// <summary>
    /// Gets the empty set.
    /// </summary>
    public static SchemaSet Empty => default;
}
