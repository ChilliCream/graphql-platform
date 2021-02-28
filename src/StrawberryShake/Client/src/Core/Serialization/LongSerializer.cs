namespace StrawberryShake.Serialization
{
    public class LongSerializer
        : ScalarSerializer<long>
    {
        public LongSerializer(string typeName = BuiltInScalarNames.Long)
            : base(typeName)
        {
        }
    }
}
