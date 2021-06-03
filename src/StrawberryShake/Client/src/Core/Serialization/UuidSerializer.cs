using System;

namespace StrawberryShake.Serialization
{
    /// <summary>
    /// This serializer handles UUID scalars.
    /// </summary>
    public class UuidSerializer : ScalarSerializer<Guid>
    {
        public UuidSerializer(string typeName = BuiltInScalarNames.Uuid)
            : base(typeName)
        {
        }
    }
}
