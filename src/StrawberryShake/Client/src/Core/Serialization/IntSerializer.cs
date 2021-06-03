namespace StrawberryShake.Serialization
{
    public class IntSerializer : ScalarSerializer<int>
    {
        public IntSerializer(string typeName = BuiltInScalarNames.Int)
            : base(typeName)
        {
        }
    }
}
