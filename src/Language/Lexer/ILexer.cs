namespace HotChocolate.Language
{
    public interface ILexer
    {
        /// <summary>
        /// Reads the <see cref="Token" />s from a GraphQL document 
        /// represented by <paramref name="source" />.
        /// </summary>
        /// <param name="source">
        /// The GraphQL document source text.
        /// </param>
        /// <returns>
        /// Returns the first token of the GraphQL document.
        /// </returns>
        /// <exception cref="SyntaxException">
        /// There are unexpected tokens in the specified source.
        /// </exception>
        Token Read(ISource source);
    }
}