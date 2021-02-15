using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class ResultFromEntityTypeMapperDescriptor
        : ICodeDescriptor
    {
        public ResultFromEntityTypeMapperDescriptor(
            NamedTypeDescriptor entityNamedType,
            NamedTypeDescriptor resultNamedType)
        {
            EntityNamedType = entityNamedType;
            ResultNamedType = resultNamedType;
        }

        public NameString Name =>
            NamingConventions.EntityMapperNameFromGraphQLTypeName(
                ResultNamedType.Name,
                ResultNamedType.GraphQLTypeName);

        /// <summary>
        /// The EntityType from which the target type shall be created
        /// </summary>
        public NamedTypeDescriptor EntityNamedType { get; }

        /// <summary>
        /// The target Result type, which is the return type of the mapper
        /// </summary>
        public NamedTypeDescriptor ResultNamedType { get; }
    }
}
