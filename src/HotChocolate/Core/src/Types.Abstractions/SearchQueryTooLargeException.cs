namespace HotChocolate;

/// <summary>
/// The exception that is thrown by an <see cref="ISchemaSearchProvider"/>
/// when a search query exceeds the maximum allowed length.
/// </summary>
public sealed class SearchQueryTooLargeException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="SearchQueryTooLargeException"/>.
    /// </summary>
    public SearchQueryTooLargeException()
        : base("The search query exceeds the maximum allowed length.")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SearchQueryTooLargeException"/>
    /// with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SearchQueryTooLargeException(string message)
        : base(message)
    {
    }
}
