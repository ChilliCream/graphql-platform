namespace StrawberryShake.Serialization
{
    public class IntSerializer : ScalarSerializer<int>
    {
        public IntSerializer(string typeName = BuiltInTypeNames.Int)
            : base(typeName)
        {
        }
    }
}
