using System;
using System.Runtime.Serialization;

namespace HotChocolate.Types
{
    [Serializable]
    public class ScalarSerializationException
        : Exception
    {
        public ScalarSerializationException(
            string message)
            : base(message) { }

        public ScalarSerializationException(
            string message, Exception innerException)
            : base(message, innerException) { }

        protected ScalarSerializationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
