namespace StrawberryShake.CodeGeneration
{
    public class OutputFieldDescriptor
        : ICodeDescriptor
    {
        public OutputFieldDescriptor(string name, string parameterName, string type)
        {
            Name = name;
            ParameterName = parameterName;
            Type = type;
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the constructor parameter that
        /// shall assign a value to this property.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Gets the type of this property.
        /// </summary>
        public string Type { get; }
    }
}
