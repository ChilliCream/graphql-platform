#pragma warning disable RCS1194 

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HotChocolate;

[Serializable]
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
        Errors = errors ?? Array.Empty<IError>();
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
                .Build()
        };
    }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    protected GraphQLException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context)
    {
        Errors = Array.Empty<IError>();
    }

    public IReadOnlyList<IError> Errors { get; }
}
