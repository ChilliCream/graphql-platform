namespace StrawberryShake.CodeGeneration
{
    public class OperationArgumentDescriptor
        : ICodeDescriptor
    {
        public OperationArgumentDescriptor(
            string name,
            string parameterName,
            string graphQLName,
            string type,
            string graphQLType,
            bool isOptional)
        {
            Name = name;
            ParameterName = parameterName;
            GraphQLName = graphQLName;
            Type = type;
            GraphQLType = graphQLType;
            IsOptional = isOptional;
        }

        public string Name { get; }

        public string ParameterName { get; }

        public string GraphQLName { get; }

        public string Type { get; }

        public string GraphQLType { get; }

        public bool IsOptional { get; }
    }
}
