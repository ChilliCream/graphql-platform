namespace Prometheus.Language
{
    internal interface ITokenReader
    {
        /// <summary>
        /// Defines if this <see cref="ITokenReader"/> is able to 
        /// handle the next token.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if this <see cref="ITokenReader"/> is able to 
        /// handle the next token, <c>false</c> otherwise.
        /// </returns>
        /// <param name="context">The lexer context.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="context"/> is <c>null</c>.
        /// </exception>
        bool CanHandle(ILexerContext context);

        /// <summary>
		/// Reads the currently selected token from the 
		/// <paramref name="context"/>.
        /// </summary>
		/// <returns>
		/// Returns the the currently selected token 
		/// read from the <paramref name="context"/>.
		/// </returns>
		/// <param name="context">The lexer context.</param>
        /// <param name="previous">The previous-token.</param>
		Token ReadToken(ILexerContext context, Token previous);
    }
}