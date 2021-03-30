using HotChocolate;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Descriptors
{
    public class ResultFromEntityTypeMapperDescriptor
        : ICodeDescriptor
    {
        public ResultFromEntityTypeMapperDescriptor(
            INamedTypeDescriptor entityNamedType,
            INamedTypeDescriptor resultNamedType)
        {
            EntityNamedType = entityNamedType;
            ResultNamedType = resultNamedType;
        }

        public NameString Name =>
            NamingConventions.CreateEntityMapperName(
                ResultNamedType.RuntimeType.Name,
                ResultNamedType.Name);

        /// <summary>
        /// The EntityType from which the target type shall be created
        /// </summary>
        public INamedTypeDescriptor EntityNamedType { get; }

        /// <summary>
        /// The target Result type, which is the return type of the mapper
        /// </summary>
        public INamedTypeDescriptor ResultNamedType { get; }
    }
}
