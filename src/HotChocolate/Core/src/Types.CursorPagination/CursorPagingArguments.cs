#nullable enable

namespace HotChocolate.Types.Pagination
{
    public readonly struct CursorPagingArguments
    {
        public CursorPagingArguments(
            int? first = null,
            int? last = null,
            string? after = null,
            string? before = null)
        {
            First = first;
            Last = last;
            After = after;
            Before = before;
        }

        public int? First { get; }

        public int? Last { get; }

        /// <summary>
        /// The cursor after which entities shall be taken.
        /// </summary>
        public string? After { get; }

        /// <summary>
        /// The cursor before which entities shall be taken.
        /// </summary>
        public string? Before { get; }
    }
}
