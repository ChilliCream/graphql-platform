namespace StrawberryShake.Serialization
{
    public class BooleanSerializer : ScalarSerializer<bool>
    {
        public BooleanSerializer(string typeName = BuiltInTypeNames.Boolean)
            : base(typeName)
        {
        }
    }
}
