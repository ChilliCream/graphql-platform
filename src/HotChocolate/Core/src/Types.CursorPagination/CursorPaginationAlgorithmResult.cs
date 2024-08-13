namespace HotChocolate.Types.Pagination;

public readonly struct CursorPaginationAlgorithmResult<TQuery>(TQuery query, int offset, int length)
    where TQuery : notnull
{
    public TQuery Query { get; } = query;

    public int Offset { get; } = offset;

    public int Length { get; } = length;

    public void Deconstruct(out TQuery query, out int offset, out int length)
    {
        query = Query;
        offset = Offset;
        length = Length;
    }
}
