using System;
using System.Runtime.Serialization;

namespace StrawberryShake
{
    [Serializable]
    public class UnknownSchemaTypeException
        : GraphQLException
    {
        public UnknownSchemaTypeException() { }

        public UnknownSchemaTypeException(string typeName)
            : base(
                $"The type `{typeName}` is unkown to the current " +
                "local schema. Update your local schema and regenerate" +
                "your client.")
        { }

        protected UnknownSchemaTypeException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
