namespace HotChocolate.Language
{
    /// <summary>
    /// Represents the GraphQL lexer. 
    /// The lexer tokenizes a GraphQL <see cref="ISource" /> and returns the first token.
    /// The tokens are chained as a a doubly linked list.
    /// </summary>
    public interface ILexer
    {
        /// <summary>
        /// Reads <see cref="Token" />s from a GraphQL 
        /// <paramref name="source" /> and returns the first token. 
        /// </summary>
        /// <param name="source">
        /// The GraphQL source that shall be tokenized.
        /// </param>
        /// <returns>
        /// Returns the first token of the given 
        /// GraphQL <paramref name="source" />.
        /// </returns>
        /// <exception cref="SyntaxException">
        /// There are unexpected tokens in the given <paramref name="source" />.
        /// </exception>
        Token Read(ISource source);
    }
}