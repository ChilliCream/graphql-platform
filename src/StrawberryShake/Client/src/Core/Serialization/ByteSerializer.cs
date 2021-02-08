namespace StrawberryShake.Serialization
{
    public class ByteSerializer : ScalarSerializer<byte, byte>
    {
        public ByteSerializer(string typeName = BuiltInTypeNames.Byte)
            : base(typeName)
        {
        }

        public override byte Parse(byte serializedValue) => serializedValue;

        protected override byte Format(byte runtimeValue) => runtimeValue;
    }
}
