using System;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public static class TypeNameHelper
    {
        public static void AddNameFunction<TDefinition>(
            IDescriptor<TDefinition> descriptor,
            Func<INamedType, NameString> createName,
            Type dependency)
            where TDefinition : DefinitionBase
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (createName is null)
            {
                throw new ArgumentNullException(nameof(createName));
            }

            if (dependency is null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }

            if (!typeof(ITypeSystemMember).IsAssignableFrom(dependency))
            {
                throw new ArgumentException(
                    TypeResources.TypeNameHelper_OnlyTsosAreAllowed,
                    nameof(dependency));
            }


            if (!dependency.IsSchemaType())
            {
                throw new ArgumentException(
                    TypeResources.TypeNameHelper_InvalidTypeStructure,
                    nameof(dependency));
            }

            descriptor
                .Extend()
                .OnBeforeNaming((ctx, definition) =>
                {
                    IType type = ctx.GetType<IType>(
                        ctx.DescriptorContext.TypeInspector.GetTypeRef(dependency));
                    definition.Name = createName(type.NamedType());
                })
                .DependsOn(dependency, true);
        }
    }
}
