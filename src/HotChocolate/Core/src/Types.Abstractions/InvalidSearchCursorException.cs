namespace HotChocolate;

/// <summary>
/// The exception that is thrown by an <see cref="ISchemaSearchProvider"/>
/// when a search cursor is invalid or cannot be decoded.
/// </summary>
public sealed class InvalidSearchCursorException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="InvalidSearchCursorException"/>.
    /// </summary>
    public InvalidSearchCursorException()
        : base("The specified search cursor is invalid.")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InvalidSearchCursorException"/>
    /// with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidSearchCursorException(string message)
        : base(message)
    {
    }
}
