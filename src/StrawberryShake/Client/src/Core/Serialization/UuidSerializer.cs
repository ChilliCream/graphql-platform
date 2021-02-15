using System;

namespace StrawberryShake.Serialization
{
    public class UuidSerializer
        : ScalarSerializer<Guid>
    {
        public UuidSerializer(string typeName = BuiltInTypeNames.Uuid)
            : base(typeName)
        {
        }
    }
}
