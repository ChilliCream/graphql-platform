namespace StrawberryShake.Serialization
{
    public class ByteArraySerializer
        : ScalarSerializer<byte[]>
    {
        public ByteArraySerializer(string typeName = BuiltInTypeNames.ByteArray)
            : base(typeName)
        {
        }
    }
}
