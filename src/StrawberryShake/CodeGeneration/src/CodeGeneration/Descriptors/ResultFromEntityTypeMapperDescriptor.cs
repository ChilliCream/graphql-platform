using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace StrawberryShake.CodeGeneration
{
    public class ResultFromEntityTypeMapperDescriptor
        : ICodeDescriptor
    {
        public string Name => NamingConventions.MapperNameFromTypeName(ResultType.Name);

        /// <summary>
        /// The EntityType from which the target type shall be created
        /// </summary>
        public TypeClassDescriptor EntityType { get; }

        /// <summary>
        /// The target Result type, which is the return type of the mapper
        /// </summary>
        public TypeClassDescriptor ResultType { get; }

        public ResultFromEntityTypeMapperDescriptor(TypeClassDescriptor entityType, TypeClassDescriptor resultType)
        {
            EntityType = entityType;
            ResultType = resultType;
        }

    }
}
