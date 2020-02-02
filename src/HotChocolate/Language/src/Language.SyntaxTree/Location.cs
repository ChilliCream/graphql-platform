namespace HotChocolate.Language
{
    public sealed class Location
    {
        public Location(int start, int end, int line, int column)
        {
            Start = start;
            End = end;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Gets the character offset at which this
        /// <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the character offset at which this
        /// <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the 1-indexed line number on which this
        /// <see cref="SyntaxToken" /> appears.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the 1-indexed column number at which this
        /// <see cref="SyntaxToken" /> begins.
        /// </summary>
        public int Column { get; }
    }
}
