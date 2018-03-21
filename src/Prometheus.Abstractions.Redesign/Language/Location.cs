namespace Prometheus.Language
{
    public class Location
    {
        /// <summary>
        /// Gets the character offset at which this <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the character offset at which this <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public int End { get; }


        /// <summary>
        /// Gets the <see cref="Token" /> at which this <see cref="ISyntaxNode" /> begins.
        /// </summary>
        public Token StartToken { get; }

        /// <summary>
        /// Gets the <see cref="Token" /> at which this <see cref="ISyntaxNode" /> ends.
        /// </summary>
        public Token EndToken { get; }

        /// <summary>
        /// Gets the <see cref="Source" /> document the AST represents.
        /// </summary>
        /// <returns></returns>
        public Source Source { get; }

    }
}