using System;
using System.Buffers;
using System.Runtime.Serialization;

namespace HotChocolate.Types.Relay;

public class IdSerializationException
    : GraphQLException
{
    [Obsolete("Use constructor with operationStatus and originalValue")]
    public IdSerializationException(string message)
        : base(message)
    {
    }

    public IdSerializationException(
        string message,
        OperationStatus operationStatus,
        string originalValue)
        : base(ErrorBuilder.New()
            .SetMessage(message)
            .SetExtension(nameof(operationStatus), operationStatus)
            .SetExtension(nameof(originalValue), originalValue)
            .Build())
    {
    }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    protected IdSerializationException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context)
    {
    }
}
