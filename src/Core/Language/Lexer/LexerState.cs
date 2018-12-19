using System;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents the internal state of a lexer session.
    /// </summary>
    internal sealed class LexerState
    {
        public LexerState(string sourceText)
        {
            SourceText = sourceText;
        }

        /// <summary>
        /// The current position of the lexer pointer.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// The number of the current line to which
        /// the lexer is currently pointing to.
        /// The line index is 1-based.
        /// </summary>
        public int Line { get; private set; } = 1;

        /// <summary>
        /// The source index of where the current line starts.
        /// </summary>
        public int LineStart { get; private set; } = 0;

        /// <summary>
        /// The column in the line where the lexer is currently pointing to.
        /// The column index is 1-based.
        /// </summary>
        public int Column { get; private set; } = 1;

        /// <summary>
        /// The normalized GraphQL source text that is beeing tokenized.
        /// </summary>
        public string SourceText { get; }

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
