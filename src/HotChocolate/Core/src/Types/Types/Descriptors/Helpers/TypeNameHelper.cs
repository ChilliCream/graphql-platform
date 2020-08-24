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
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (createName == null)
            {
                throw new ArgumentNullException(nameof(createName));
            }

            if (dependency == null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }

            if (!typeof(ITypeSystemMember).IsAssignableFrom(dependency))
            {
                throw new ArgumentException(
                    TypeResources.TypeNameHelper_OnlyTsosAreAllowed,
                    nameof(dependency));
            }

            if (!TypeInfo.TryCreate(dependency, out TypeInfo typeInfo) ||
                !typeInfo.IsSchemaType)
            {
                throw new ArgumentException(
                    TypeResources.TypeNameHelper_InvalidTypeStructure,
                    nameof(dependency));
            }

            descriptor
                .Extend()
                .OnBeforeNaming((ctx, definition) =>
                {
                    INamedType type = ctx.GetType<INamedType>(
                        TypeReference.Create(typeInfo.GetExtendedType()));
                    definition.Name = createName(type);
                })
                .DependsOn(dependency, true);
        }
    }
}
