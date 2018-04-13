using System;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents the internal state of a lexer session.
    /// </summary>
    internal sealed class LexerState
    {
        /// <summary>
        /// The current position of the lexer pointer.
        /// </summary>
        public int Position;

        /// <summary>
        /// The number of the current line to which 
        /// the lexer is currently pointing to.
        /// The line index is 1-based.
        /// </summary>
        public int Line = 1;

        /// <summary>
        /// The source index of where the current line starts.
        /// </summary>
        public int LineStart = 0;

        /// <summary>
        /// The column in the line where the lexer is currently pointing to.
        /// </summary>
        public int Column = 1;

        /// <summary>
        /// The normalized GraphQL source text that is beeing tokenized.
        /// </summary>
        public string SourceText;

        
        public void NewLine()
        {
            Line++;
            LineStart = Position;
            UpdateColumn();
        }

        public void NewLine(int lines)
        {
            if (lines < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lines), "must be greater or equal to 1.");
            }

            Line += lines;
            LineStart = Position;
            UpdateColumn();
        }

        public void UpdateColumn()
        {
            Column = 1 + Position - LineStart;
        }

        public bool IsEndOfStream()
        {
            return !(Position < SourceText.Length);
        }
    }
}