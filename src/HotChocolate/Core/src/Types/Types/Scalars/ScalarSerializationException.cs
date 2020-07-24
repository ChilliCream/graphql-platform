using System;
using System.Runtime.Serialization;

namespace HotChocolate.Types
{
    [Serializable]
    public class ScalarSerializationException
        : GraphQLException
    {
        public ScalarSerializationException(string message)
            : base(message) { }

        public ScalarSerializationException(IError error)
            : base(error) { }

        protected ScalarSerializationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
