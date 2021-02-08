namespace StrawberryShake.Serialization
{
    public class DecimalSerializer : ScalarSerializer<decimal, decimal>
    {
        public DecimalSerializer(string typeName = BuiltInTypeNames.Decimal)
            : base(typeName)
        {
        }

        public override decimal Parse(decimal serializedValue) => serializedValue;

        protected override decimal Format(decimal runtimeValue) => runtimeValue;
    }
}
