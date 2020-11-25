using System;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public class IdFieldValueSerializerFactory : IIdFieldValueSerializerFactory
    {
        public IdFieldValueSerializer Create(
            NameString typeName,
            IIdSerializer innerSerializer,
            bool validateType,
            bool isListType,
            Type valueType)
        {
            return new IdFieldValueSerializer(
                typeName,
                innerSerializer,
                validateType,
                isListType,
                valueType);
        }
    }
}
