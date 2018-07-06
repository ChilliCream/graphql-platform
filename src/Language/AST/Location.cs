using System;

namespace HotChocolate.Language
{
    public sealed class Location
    {
        public Location(
            ISource source,
            SyntaxToken start,
            SyntaxToken end)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (end == null)
            {
                throw new ArgumentNullException(nameof(end));
            }

            StartToken = start;
            EndToken = end;
            Source = source;
            Start = start.Start;
            End = end.End;
        }

        /// <summary>
        /// Gets the character offset at which this <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the character offset at which this <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the <see cref="SyntaxToken" /> at which this <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public SyntaxToken StartToken { get; }

        /// <summary>
        /// Gets the <see cref="SyntaxToken" /> at which this <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public SyntaxToken EndToken { get; }

        /// <summary>
        /// Gets the <see cref="Source" /> document the AST represents.
        /// </summary>
        /// <returns></returns>
        public ISource Source { get; }
    }
}
