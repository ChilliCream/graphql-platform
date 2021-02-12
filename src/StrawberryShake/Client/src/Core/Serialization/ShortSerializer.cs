namespace StrawberryShake.Serialization
{
    public class ShortSerializer : ScalarSerializer<short>
    {
        public ShortSerializer(string typeName = BuiltInTypeNames.Short)
            : base(typeName)
        {
        }
    }
}
