namespace StrawberryShake.CodeGeneration
{
    public class ValueSerializerDescriptor
        : ICodeDescriptor
    {
        public ValueSerializerDescriptor(string name, string fieldName)
        {
            Name = name;
            FieldName = fieldName;
        }

        public string Name { get; }

        public string FieldName { get; }
    }
}
