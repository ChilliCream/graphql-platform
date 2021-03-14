namespace StrawberryShake.Serialization
{
    public class ByteArraySerializer : ScalarSerializer<byte[]>
    {
        public ByteArraySerializer(string typeName = BuiltInScalarNames.ByteArray)
            : base(typeName)
        {
        }
    }
}
