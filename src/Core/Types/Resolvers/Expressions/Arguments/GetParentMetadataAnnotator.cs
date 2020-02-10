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
                return sourceType
                    .GetProperties()
                    .Aggregate(metadata,
                        (m, property) => m.WithDependsOn(property.Name));
            }
            else
            {
                return metadata.WithDependsOn(attribute.Property);
            }
        }
    }
}
