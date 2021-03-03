namespace StrawberryShake.Serialization
{
    public class IdSerializer : ScalarSerializer<string>
    {
        public IdSerializer(string typeName = BuiltInScalarNames.ID)
            : base(typeName)
        {
        }
    }
}
