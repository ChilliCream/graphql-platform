namespace HotChocolate.AspNetCore;

public class GraphQLRequestException : GraphQLException
{
    public GraphQLRequestException(string message)
        : base(message)
    {
    }

    public GraphQLRequestException(IError error)
        : base(error)
    {
    }

    public GraphQLRequestException(IError error, Exception innerException)
        : base(error, innerException)
    {
    }

    public GraphQLRequestException(params IError[] errors)
        : base(errors)
    {
    }

    public GraphQLRequestException(IEnumerable<IError> errors)
        : base(errors)
    {
    }
}
