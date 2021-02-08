namespace StrawberryShake.Serialization
{
    public class BooleanSerializer : ScalarSerializer<bool, bool>
    {
        public BooleanSerializer(string typeName = BuiltInTypeNames.Boolean)
            : base(typeName)
        {
        }

        public override bool Parse(bool serializedValue) => serializedValue;

        protected override bool Format(bool runtimeValue) => runtimeValue;
    }
}
