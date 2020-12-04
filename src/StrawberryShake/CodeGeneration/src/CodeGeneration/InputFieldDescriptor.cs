namespace StrawberryShake.CodeGeneration
{
    public class InputFieldDescriptor
        : ICodeDescriptor
    {
        public InputFieldDescriptor(string name, string type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of this property.
        /// </summary>
        public string Type { get; }
    }
}
