namespace StrawberryShake.Serialization
{
    public class DecimalSerializer : ScalarSerializer<decimal>
    {
        public DecimalSerializer(string typeName = BuiltInTypeNames.Decimal)
            : base(typeName)
        {
        }
    }
}
