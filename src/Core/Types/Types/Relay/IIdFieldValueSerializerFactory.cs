using System;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public interface IIdFieldValueSerializerFactory
    {
        IdFieldValueSerializer Create(
            NameString typeName,
            IIdSerializer innerSerializer,
            bool validateType,
            bool isListType,
            Type valueType);
    }
}
