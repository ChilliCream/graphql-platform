using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Helpers;

public static class TypeNameHelper
{
    public static void AddNameFunction<TDefinition>(
        IDescriptor<TDefinition> descriptor,
        Func<ITypeDefinition, string> createName,
        Type dependency)
        where TDefinition : TypeSystemConfiguration, ITypeConfiguration
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
                TypeResources.TypeNameHelper_OnlyTypeSystemObjectsAreAllowed,
                nameof(dependency));
        }

        if (!dependency.IsSchemaType())
        {
            throw new ArgumentException(
                TypeResources.TypeNameHelper_InvalidTypeStructure,
                nameof(dependency));
        }

        descriptor.Extend().Configuration.NeedsNameCompletion = true;

        descriptor
            .Extend()
            .OnBeforeNaming((ctx, definition) =>
            {
                var typeRef = ctx.DescriptorContext.TypeInspector.GetTypeRef(dependency);
                var type = ctx.GetType<IType>(typeRef);
                definition.Name = createName(type.NamedType());
            })
            .DependsOn(dependency, true);
    }

    public static void AddNameFunction<TDefinition>(
        IDescriptor<TDefinition> descriptor,
        Func<ITypeDefinition, string> createName,
        TypeReference dependency)
        where TDefinition : TypeSystemConfiguration, ITypeConfiguration
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

        descriptor.Extend().Configuration.NeedsNameCompletion = true;

        descriptor
            .Extend()
            .OnBeforeNaming((ctx, definition) =>
            {
                var type = ctx.GetType<IType>(dependency);
                definition.Name = createName(type.NamedType());
            })
            .DependsOn(dependency, true);
    }
}
