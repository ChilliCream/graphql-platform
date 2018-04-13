using System;

namespace HotChocolate.Language
{
    public sealed class Token
    {
        public Token(
            TokenKind kind,
            int start, int end,
            int line, int column,
            Token previous)
        {
            Kind = kind;
            Start = start;
            End = end;
            Line = line;
            Column = column;
            Previous = previous;
        }

        public Token(
           TokenKind kind,
           int start, int end,
           int line, int column,
           string value,
           Token previous)
        {
            Kind = kind;
            Start = start;
            End = end;
            Line = line;
            Column = column;
            Value = value;
            Previous = previous;
        }

        /// <summary>
        /// Gets the kind of <see cref="Token" />.
        /// </summary>
        public TokenKind Kind { get; }

        /// <summary>
        /// Gets the character offset at which this node begins.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the character offset at which this node ends.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the 1-indexed line number on which this <see cref="Token" /> appears.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the 1-indexed column number at which this <see cref="Token" /> begins.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// For non-punctuation tokens, represents the interpreted value of the token.
        /// </summary>
        public string Value { get; }

        public Token Previous { get; }

        public Token Next { get; internal set; }

        public override string ToString()
        {
            return TokenVisualizer.Visualize(this);
        }
    }
}