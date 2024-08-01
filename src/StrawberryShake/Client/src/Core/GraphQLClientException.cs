using System.Text;

namespace StrawberryShake;

/// <summary>
/// GraphQL client-side error.
/// </summary>
public class GraphQLClientException : Exception
{
    /// <summary>
    /// Creates a new exception with the specified message.
    /// </summary>
    /// <param name="message">
    /// The error message.
    /// </param>
    public GraphQLClientException(string message)
        : this(new ClientError(message))
    {
    }

    /// <summary>
    /// Creates a new exception that is caused by the specified client <paramref name="error"/>.
    /// </summary>
    /// <param name="error">
    /// The client error.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="error"/> is <c>null</c>.
    /// </exception>
    public GraphQLClientException(IClientError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Message = error.Message;
        Errors = new[] { error, };
    }

    /// <summary>
    /// Creates a new exception that is caused by the specified client <paramref name="errors"/>.
    /// </summary>
    /// <param name="errors">
    /// The client errors.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public GraphQLClientException(params IClientError[] errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        if (errors.Length == 0)
        {
            Message = "Unknown error occurred";
        }
        else if (errors.Length == 1)
        {
            Message = errors[0].Message;
        }
        else
        {
            var message = new StringBuilder("Multiple errors occurred:");

            foreach (var error in errors)
            {
                message.AppendLine();
                message.Append("- ");
                message.Append(error.Message);
            }

            Message = message.ToString();
        }

        Errors = errors;
    }

    /// <summary>
    /// Creates a new exception that is caused by the specified client <paramref name="errors"/>.
    /// </summary>
    /// <param name="errors">
    /// The client errors.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="errors"/> is <c>null</c>.
    /// </exception>
    public GraphQLClientException(IEnumerable<IClientError> errors)
        // We pass this null safe to the constructor using arrays and let it throw there
        // with a proper ArgumentNullException.
        : this(errors?.ToArray()!)
    {
    }

    /// <summary>
    /// The aggregated error message.
    /// </summary>
    public sealed override string Message { get; }

    /// <summary>
    /// The underlying client errors.
    /// </summary>
    public IReadOnlyList<IClientError> Errors { get; }
}
