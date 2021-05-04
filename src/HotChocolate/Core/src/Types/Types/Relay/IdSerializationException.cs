using System;
using System.Buffers;
using System.Runtime.Serialization;

namespace HotChocolate.Types.Relay
{
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

        protected IdSerializationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
