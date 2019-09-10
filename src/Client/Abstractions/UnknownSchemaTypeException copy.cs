using System;
using System.Runtime.Serialization;

namespace StrawberryShake
{
    [Serializable]
    public class SerializerNotFoundException
        : GraphQLException
    {
        public SerializerNotFoundException() { }

        public SerializerNotFoundException(string typeName)
            : base(
                $"There is no serializer configured for type `{typeName}`." +
                "You can create custom serializer by implementing " +
                "IValueSerializer. Custom serializer then have to be registerd " +
                "with your dependency injection by using out extension method " +
                "`services.AddValueSerialzer<YourCustomSerializer>()`.")
        { }

        protected SerializerNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
