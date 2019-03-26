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
            Line = 1;
            Column = 1;
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
        /// Gets the 1-indexed line number on which this
        /// <see cref="SyntaxToken" /> appears.
        /// </summary>
        public int Line { get; private set; }

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

    }
}
