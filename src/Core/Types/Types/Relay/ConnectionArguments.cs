#nullable enable

namespace HotChocolate.Types.Relay
{
    public readonly struct ConnectionArguments
    {
        public ConnectionArguments(int? first, int? last, string? after, string? before)
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
