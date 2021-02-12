namespace StrawberryShake.Serialization
{
    public class FloatSerializer : ScalarSerializer<double>
    {
        public FloatSerializer(string typeName = BuiltInTypeNames.Float)
            : base(typeName)
        {
        }
    }
}
