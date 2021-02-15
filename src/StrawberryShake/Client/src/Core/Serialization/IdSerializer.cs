namespace StrawberryShake.Serialization
{
    public class IdSerializer : ScalarSerializer<string>
    {
        public IdSerializer(string typeName = BuiltInTypeNames.ID)
            : base(typeName)
        {
        }
    }
}
