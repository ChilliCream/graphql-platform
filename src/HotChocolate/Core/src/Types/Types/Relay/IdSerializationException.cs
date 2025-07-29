using System.Buffers;

namespace HotChocolate.Types.Relay;

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
