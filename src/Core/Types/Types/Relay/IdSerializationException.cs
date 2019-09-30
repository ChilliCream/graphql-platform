using System.Runtime.Serialization;

namespace HotChocolate.Types.Relay
{
    public class IdSerializationException
        : GraphQLException
    {
        public IdSerializationException(string message)
            : base(message)
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
