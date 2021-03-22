namespace StrawberryShake.Serialization
{
    public class ByteSerializer : ScalarSerializer<byte>
    {
        public ByteSerializer(string typeName = BuiltInScalarNames.Byte)
            : base(typeName)
        {
        }
    }
}
