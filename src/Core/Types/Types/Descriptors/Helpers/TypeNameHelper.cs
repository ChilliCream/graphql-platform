using System;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

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

            if (!NamedTypeInfoFactory.Default.TryCreate(
                dependency,
                out TypeInfo typeInfo))
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
                        ClrTypeReference.FromSchemaType(typeInfo.ClrType));
                    definition.Name = createName(type);
                })
                .DependsOn(dependency, true);
        }
    }
}
