namespace HotChocolate.Types.Pagination;

internal sealed class CursorPagingRange(int start, int end)
{
    public int Start { get; private set; } = start;

    public int End { get; private set; } = end;

    public int Count()
    {
        if (End < Start)
        {
            return 0;
        }

        return End - Start;
    }

    public void Take(int? first)
    {
        if (first is not null)
        {
            var end = Start + first.Value;
            if (End > end)
            {
                End = end;
            }
        }
    }

    public void TakeLast(int? last)
    {
        if (last is not null)
        {
            var start = End - last.Value;
            if (Start < start)
            {
                Start = start;
            }
        }
    }
}
