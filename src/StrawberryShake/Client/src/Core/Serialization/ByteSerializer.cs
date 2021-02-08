namespace StrawberryShake.Serialization
{
    public class ByteSerializer : ScalarSerializer<byte>
    {
        public ByteSerializer(string typeName = BuiltInTypeNames.Byte)
            : base(typeName)
        {
        }
    }
}
