using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Helpers;

public static class TypeNameHelper
{
    public static void AddNameFunction<TDefinition>(
        IDescriptor<TDefinition> descriptor,
        Func<INamedType, string> createName,
        Type dependency)
        where TDefinition : DefinitionBase, ITypeDefinition
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

        descriptor.Extend().Definition.NeedsNameCompletion = true;

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
}
