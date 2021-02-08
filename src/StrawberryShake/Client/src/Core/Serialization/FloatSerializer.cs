namespace StrawberryShake.Serialization
{
    public class FloatSerializer : ScalarSerializer<double, double>
    {
        public FloatSerializer(string typeName = BuiltInTypeNames.Float)
            : base(typeName)
        {
        }

        public override double Parse(double serializedValue) => serializedValue;

        protected override double Format(double runtimeValue) => runtimeValue;
    }
}
