using System;

namespace StrawberryShake.Serialization
{
    /// <summary>
    /// This serializer handles UUID scalars.
    /// </summary>
    public class UUIDSerializer : ScalarSerializer<Guid>
    {
        public UUIDSerializer(string typeName = BuiltInScalarNames.UUID)
            : base(typeName)
        {
        }
    }
}
