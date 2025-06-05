namespace HotChocolate;

/// <summary>
/// Represents a GraphQL execution error.
/// </summary>
public class GraphQLException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLException"/> class.
    /// </summary>
    public GraphQLException(string message)
        : this(ErrorBuilder.New().SetMessage(message).Build())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLException"/> class.
    /// </summary>
    /// <param name="error">The error.</param>
    public GraphQLException(IError error)
        : base(error.Message)
    {
        Errors = [error];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLException"/> class.
    /// </summary>
    /// <param name="errors">The errors.</param>
    public GraphQLException(params IError[] errors)
    {
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLException"/> class.
    /// </summary>
    /// <param name="errors">The errors.</param>
    public GraphQLException(IEnumerable<IError> errors)
    {
        Errors = new List<IError>(errors).AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public GraphQLException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors =
        [
            ErrorBuilder.New()
                .SetMessage(message)
                .SetException(innerException)
                .Build()
        ];
    }

    /// <summary>
    /// Gets the GraphQL execution errors.
    /// </summary>
    public IReadOnlyList<IError> Errors { get; }
}
