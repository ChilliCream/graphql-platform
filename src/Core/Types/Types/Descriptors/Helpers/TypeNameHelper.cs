using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    internal static class TypeNameHelper
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

            if (!typeof(ITypeSystem).IsAssignableFrom(dependency))
            {
                // TODO : resources
                throw new ArgumentException(
                    "Only type system objects are allowed.");
            }

            if (!NamedTypeInfoFactory.Default.TryCreate(
                dependency,
                out TypeInfo typeInfo))
            {
                // TODO : Resources
                throw new ArgumentException("Invalid type structure.");
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
