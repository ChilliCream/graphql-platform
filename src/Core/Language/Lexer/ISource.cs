namespace HotChocolate.Language
{
    /// <summary>
    /// Represents a GraphQL source.
    /// </summary>
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
