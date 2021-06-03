namespace StrawberryShake.Serialization
{
    public class FloatSerializer : ScalarSerializer<double>
    {
        public FloatSerializer(string typeName = BuiltInScalarNames.Float)
            : base(typeName)
        {
        }
    }
}
