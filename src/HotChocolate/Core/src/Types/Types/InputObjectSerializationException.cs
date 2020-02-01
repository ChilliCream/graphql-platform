using System;
using System.Runtime.Serialization;

namespace HotChocolate.Types
{
    [Serializable]
    public class InputObjectSerializationException
        : Exception
    {
        public InputObjectSerializationException(
            string message)
            : base(message) { }

        public InputObjectSerializationException(
            string message, Exception innerException)
            : base(message, innerException) { }

        protected InputObjectSerializationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
