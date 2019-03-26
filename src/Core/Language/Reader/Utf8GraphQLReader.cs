using System;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLReader
    {
        public Utf8GraphQLReader(ReadOnlySpan<byte> graphQLData)
        {
            GraphQLData = graphQLData;
            Kind = TokenKind.StartOfFile;
            Start = 0;
            End = 0;
            LineStart = 0;
            Position = 0;
            Line = 1;
            Column = 1;
            Value = null;
        }

        public ReadOnlySpan<byte> GraphQLData { get; }

        /// <summary>
        /// Gets the kind of <see cref="SyntaxToken" />.
        /// </summary>
        public TokenKind Kind { get; private set; }

        /// <summary>
        /// Gets the character offset at which this node begins.
        /// </summary>
        public int Start { get; private set; }

        /// <summary>
        /// Gets the character offset at which this node ends.
        /// </summary>
        public int End { get; private set; }

        /// <summary>
        /// The current position of the lexer pointer.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets the 1-indexed line number on which this
        /// <see cref="SyntaxToken" /> appears.
        /// </summary>
        public int Line { get; private set; }

        /// <summary>
        /// The source index of where the current line starts.
        /// </summary>
        public int LineStart { get; private set; }

        /// <summary>
        /// Gets the 1-indexed column number at which this
        /// <see cref="SyntaxToken" /> begins.
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// For non-punctuation tokens, represents the interpreted
        /// value of the token.
        /// </summary>
        public ReadOnlySpan<byte> Value { get; private set; }


        public bool Read()
        {

            return false;
        }

        /// <summary>
        /// Sets the state to a new line.
        /// </summary>
        public void NewLine()
        {
            Line++;
            LineStart = Position;
            UpdateColumn();
        }

        /// <summary>
        /// Sets the state to a new line.
        /// </summary>
        /// <param name="lines">
        /// The number of lines to skip.
        /// </param>
        public void NewLine(int lines)
        {
            if (lines < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lines),
                    "Must be greater or equal to 1.");
            }

            Line += lines;
            LineStart = Position;
            UpdateColumn();
        }

        /// <summary>
        /// Updates the column index.
        /// </summary>
        public void UpdateColumn()
        {
            Column = 1 + Position - LineStart;
        }

        /// <summary>
        /// Checks if the lexer source pointer has reached
        /// the end of the GraphQL source text.
        /// </summary>
        /// <returns></returns>
        public bool IsEndOfStream()
        {
            return Position >= SourceText.Length;
        }
    }
}
