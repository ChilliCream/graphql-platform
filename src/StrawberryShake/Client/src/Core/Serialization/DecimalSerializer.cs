namespace StrawberryShake.Serialization
{
    public class DecimalSerializer : ScalarSerializer<decimal>
    {
        public DecimalSerializer(string typeName = BuiltInScalarNames.Decimal)
            : base(typeName)
        {
        }
    }
}
