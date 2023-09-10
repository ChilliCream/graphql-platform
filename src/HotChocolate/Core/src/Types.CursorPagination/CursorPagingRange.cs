namespace HotChocolate.Types.Pagination;

internal sealed class CursorPagingRange
{
    public CursorPagingRange(int start, int end)
    {
        Start = start;
        End = end;
    }

    public int Start { get; private set; }

    public int End { get; private set; }

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
        if (first is { })
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
        if (last is { })
        {
            var start = End - last.Value;
            if (Start < start)
            {
                Start = start;
            }
        }
    }
}
