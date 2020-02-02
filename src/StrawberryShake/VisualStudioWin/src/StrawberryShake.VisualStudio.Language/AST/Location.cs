namespace StrawberryShake.VisualStudio.Language
{
    public readonly struct Location
    {
        public Location(
            ISyntaxToken start,
            ISyntaxToken end)
        {
            Start = start.Start;
            StartToken = start;
            End = end.End;
            EndToken = end;
            Length = end.End - start.Start;
            Line = start.Line;
            Column = start.Column;
        }

        /// <summary>
        /// Gets the character offset at which this
        /// <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the start token at which this
        /// <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public ISyntaxToken StartToken { get; }


        /// <summary>
        /// Gets the character offset at which this
        /// <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the end token at which this
        /// <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public ISyntaxToken EndToken { get; }

        /// <summary>
        /// Gets the length of the <see cref="ISyntaxNode" />.
        /// </summary>
        /// <value></value>
        public int Length { get; }

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
