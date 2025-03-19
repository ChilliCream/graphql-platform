using System.Buffers;

namespace HotChocolate.Types.Relay;

#pragma warning disable RCS1194
public class IdSerializationException
    : GraphQLException
{
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
}
#pragma warning restore RCS1194
