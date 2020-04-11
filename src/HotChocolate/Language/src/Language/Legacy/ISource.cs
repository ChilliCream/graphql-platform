using System;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents a GraphQL source.
    /// </summary>
    [Obsolete("Use the Utf8GraphQLParser.")]
    public interface ISource
    {
        /// <summary>
        /// Gets the GraphQL source text.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL source text.
        /// </returns>
        string Text { get; }
    }
}
