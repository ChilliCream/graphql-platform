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

        /// <summary>
        /// Gets the name of the GraphQL scalar.
        /// </summary>
        /// <value></value>
        public string Name { get; }

        public string FieldName { get; }
    }
}
