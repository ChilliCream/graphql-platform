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
            StartToken = start 
                ?? throw new ArgumentNullException(nameof(start));
            EndToken = end 
                ?? throw new ArgumentNullException(nameof(end));
            Source = source 
                ?? throw new ArgumentNullException(nameof(source));
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
        public SyntaxToken StartToken { get; }

        /// <summary>
        /// Gets the <see cref="SyntaxToken" /> at which this
        /// <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public SyntaxToken EndToken { get; }

        /// <summary>
        /// Gets the <see cref="Source" /> document the AST represents.
        /// </summary>
        /// <returns></returns>
        public ISource Source { get; }
    }
}
