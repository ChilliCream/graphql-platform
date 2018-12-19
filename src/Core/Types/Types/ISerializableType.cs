using System;

namespace HotChocolate.Types
{
    public interface ISerializableType
        : IType
    {
        /// <summary>
        /// Serializes an instance of this type to
        /// the specified serialization type.
        /// </summary>
        object Serialize(object value);

        /// <summary>
        /// Deserializes a serialized instance of this type.
        /// </summary>
        object Deserialize(object value);

        /// <summary>
        /// Tries to deserialize a serialized instance of this type.
        /// </summary>
        bool TryDeserialize(object serialized, out object value);
    }
}
