namespace HotChocolate.Language;

/// <summary>
/// An exception that is thrown when a GraphQL request has an invalid structure or format.
/// </summary>
public class InvalidGraphQLRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="InvalidGraphQLRequestException"/>.
    /// </summary>
    public InvalidGraphQLRequestException()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InvalidGraphQLRequestException"/>.
    /// </summary>
    /// <param name="message">
    /// The error message that explains the reason for the exception.
    /// </param>
    public InvalidGraphQLRequestException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InvalidGraphQLRequestException"/>.
    /// </summary>
    /// <param name="message">
    /// The error message that explains the reason for the exception.
    /// </param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception.
    /// </param>
    public InvalidGraphQLRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
