namespace StrawberryShake.Serialization
{
    public class ShortSerializer : ScalarSerializer<short>
    {
        public ShortSerializer(string typeName = BuiltInScalarNames.Short)
            : base(typeName)
        {
        }
    }
}
