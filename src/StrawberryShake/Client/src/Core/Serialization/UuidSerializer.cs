using System;

namespace StrawberryShake.Serialization
{
    /// <summary>
    /// This serializer handles UUID scalars.
    /// </summary>
    public class UuidSerializer : ScalarSerializer<string, Guid>
    {
        private readonly string _format;

        public UuidSerializer(string typeName = BuiltInScalarNames.Uuid, string format = "D")
            : base(typeName)
        {
            _format = format;
        }

        public override Guid Parse(string serializedValue)
        {
            if (Guid.TryParse(serializedValue, out Guid guid))
            {
                return guid;
            }
            throw ThrowHelper.UuidSerializer_CouldNotParse(serializedValue);
        }

        protected override string Format(Guid runtimeValue)
        {
            return runtimeValue.ToString(_format);
        }
    }
}
