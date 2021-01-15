using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace StrawberryShake.CodeGeneration
{
    public class ResultFromEntityTypeMapperDescriptor
        : ICodeDescriptor
    {
        public string Name => NamingConventions.MapperNameFromGraphQlTypeName(ResultType.Name);

        /// <summary>
        /// The EntityType from which the target type shall be created
        /// </summary>
        public TypeDescriptor EntityType { get; }

        /// <summary>
        /// The target Result type, which is the return type of the mapper
        /// </summary>
        public TypeDescriptor ResultType { get; }

        public ResultFromEntityTypeMapperDescriptor(TypeDescriptor entityType, TypeDescriptor resultType)
        {
            EntityType = entityType;
            ResultType = resultType;
        }

    }
}
