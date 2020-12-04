namespace StrawberryShake.CodeGeneration
{
    public class ClientOperationMethodParameterDescriptor
        : ICodeDescriptor
    {
        public ClientOperationMethodParameterDescriptor(
            string name,
            string propertyName,
            string typeName,
            bool isOptional = false,
            string? @default = null)
        {
            Name = name;
            PropertyName = propertyName;
            TypeName = typeName;
            IsOptional = isOptional;
            Default = @default;
        }

        public string Name { get; }

        public string PropertyName { get; }

        public string TypeName { get; }

        public bool IsOptional { get; }

        public string? Default { get; }
    }
}
