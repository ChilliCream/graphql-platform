namespace HotChocolate.Types.Pagination;

/// <summary>
/// This result class represents the sliced query.
/// </summary>
/// <param name="query">
/// The query that was sliced.
/// </param>
/// <param name="offset">
/// The offset the first entity in the result set.
/// </param>
/// <param name="length">
/// The maximum length of the result set.
/// </param>
/// <typeparam name="TQuery">
/// The type of this query.
/// </typeparam>
public readonly struct CursorPaginationAlgorithmResult<TQuery>(TQuery query, int offset, int length)
    where TQuery : notnull
{
    /// <summary>
    /// Gets the sliced query.
    /// </summary>
    public TQuery Query { get; } = query;

    /// <summary>
    /// Gets the offset of the first entity in the result set.
    /// </summary>
    public int Offset { get; } = offset;

    /// <summary>
    /// Gets the maximum length of the result set.
    /// </summary>
    public int Length { get; } = length;

    public void Deconstruct(out TQuery query, out int offset, out int length)
    {
        query = Query;
        offset = Offset;
        length = Length;
    }
}
