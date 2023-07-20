using System.Runtime.Serialization;

namespace HotChocolate.AspNetCore;

[Serializable]
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

    public GraphQLRequestException(params IError[] errors)
        : base(errors)
    {

    }

    public GraphQLRequestException(IEnumerable<IError> errors)
        : base(errors)
    {
    }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    protected GraphQLRequestException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context)
    {
    }
}
