using System;

namespace HotChocolate.Language
{
    public sealed class Location
    {
        public Location(
            SyntaxTokenInfo start,
            SyntaxTokenInfo end)
        {
            StartToken = start;
            EndToken = end;
            Start = start.Start;
            End = end.End;
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
        /// Gets the <see cref="SyntaxToken" /> at which this
        /// <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public SyntaxTokenInfo StartToken { get; }

        /// <summary>
        /// Gets the <see cref="SyntaxToken" /> at which this
        /// <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public SyntaxTokenInfo EndToken { get; }
    }
}
