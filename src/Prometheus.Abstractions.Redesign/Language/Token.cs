using System;

namespace Prometheus.Language
{

    public class Token
    {
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

        public Token Next { get; }
    }

    internal class TokenConfig
    {
        public TokenConfig(
            TokenKind kind, 
            int start, int end, 
            int line, int column, 
            TokenConfig previous)
        {
            Kind = kind;
            Start = start;
            End = end;
            Line = line;
            Column = column;
            Previous = previous;
        }

        /// <summary>
        /// Gets the kind of <see cref="Token" />.
        /// </summary>
        public TokenKind Kind { get; set; }

        /// <summary>
        /// Gets the character offset at which this node begins.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Gets the character offset at which this node ends.
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// Gets the 1-indexed line number on which this <see cref="Token" /> appears.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Gets the 1-indexed column number at which this <see cref="Token" /> begins.
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// For non-punctuation tokens, represents the interpreted value of the token.
        /// </summary>
        public string Value { get; set; }

        public TokenConfig Previous { get; set; }

        public Func<TokenConfig> Next { get; set; }
    }
}