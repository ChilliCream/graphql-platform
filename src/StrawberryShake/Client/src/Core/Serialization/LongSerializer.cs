namespace StrawberryShake.Serialization
{
    public class LongSerializer
        : ScalarSerializer<long>
    {
        public LongSerializer(string typeName = BuiltInTypeNames.Long)
            : base(typeName)
        {
        }
    }
}
