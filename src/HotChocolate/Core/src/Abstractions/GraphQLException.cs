namespace HotChocolate;

public class GraphQLException : Exception
{
    public GraphQLException(string message)
        : this(ErrorBuilder.New().SetMessage(message).Build())
    {
    }

    public GraphQLException(IError error)
        : base(error?.Message)
    {
        Errors = error is null
            ? Array.Empty<IError>()
            : [error,];
    }

    public GraphQLException(params IError[] errors)
    {
        Errors = errors ?? [];
    }

    public GraphQLException(IEnumerable<IError> errors)
    {
        Errors = new List<IError>(
           errors ?? Array.Empty<IError>())
               .AsReadOnly();
    }

    public GraphQLException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new[]
        {
            ErrorBuilder.New()
                .SetMessage(message)
                .SetException(innerException)
                .Build(),
        };
    }

    public IReadOnlyList<IError> Errors { get; }
}
