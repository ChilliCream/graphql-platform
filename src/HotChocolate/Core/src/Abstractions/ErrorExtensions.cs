using HotChocolate.Language;

#nullable enable
namespace HotChocolate
{
    /// <summary>
    /// Common extensions of <see cref="IError"/>
    /// </summary>
    public static class ErrorExtensions
    {
        /// <summary>
        /// Creates a new error that contains all properties of this error
        /// but removed the syntax node from it.
        /// </summary>
        /// <param name="error">The error this extension method applies to</param>
        /// <returns>
        /// Returns a new error that contains all properties of this error
        /// but without any syntax node details.
        /// </returns>
        public static IError RemoveSyntaxNode(this IError error)
        {
            return new Error(error.Message,
                error.Code,
                error.Path,
                error.Locations,
                error.Extensions,
                error.Exception);
        }

        /// <summary>
        /// Creates a new error that contains all properties of this error
        /// but with the specified <paramref name="syntaxNode" />.
        /// </summary>
        /// <param name="error">The error this extension method applies to</param>
        /// <param name="syntaxNode">
        /// The .net syntaxNode that caused this error.
        /// </param>
        /// <returns>
        /// Returns a new error that contains all properties of this error
        /// but with the specified <paramref name="syntaxNode" />.
        /// </returns>
        public static IError WithSyntaxNode(this IError error, ISyntaxNode? syntaxNode)
        {
            return new Error(error.Message,
                error.Code,
                error.Path,
                error.Locations,
                error.Extensions,
                error.Exception,
                syntaxNode);
        }
    }
}
