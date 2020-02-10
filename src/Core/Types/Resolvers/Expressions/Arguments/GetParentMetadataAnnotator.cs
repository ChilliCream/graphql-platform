using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal class GetParentMetadataAnnotator<T>
        : ResolverMetadataAnnotatorBase<T>
        where T : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            ArgumentHelper.IsParent(parameter, sourceType);

        public override ResolverMetadata Annotate(
            ResolverMetadata metadata,
            ParameterInfo parameter,
            Type sourceType)
        {
            ParentAttribute attribute = parameter.GetCustomAttributes<ParentAttribute>()
               .FirstOrDefault();

            if (attribute?.Property == null)
            {
                return metadata.AsNonPure();
            }
            else
            {
                PropertyInfo property = sourceType.GetProperty(attribute.Property);

                return metadata.WithDependsOn(property);
            }
        }
    }
}
