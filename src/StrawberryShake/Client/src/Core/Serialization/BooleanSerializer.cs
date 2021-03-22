namespace StrawberryShake.Serialization
{
    public class BooleanSerializer : ScalarSerializer<bool>
    {
        public BooleanSerializer(string typeName = BuiltInScalarNames.Boolean)
            : base(typeName)
        {
        }
    }
}
