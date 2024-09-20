using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class DeferredFragmentMapper
{
    public static void Map(IMapperContext context)
    {
        foreach (var complexType in context.Types.OfType<ComplexTypeDescriptor>())
        {
            foreach (var fragmentDescriptor in complexType.Deferred)
            {
                fragmentDescriptor.Complete(
                    context.GetType<InterfaceTypeDescriptor>(fragmentDescriptor.InterfaceName),
                    context.GetType<ObjectTypeDescriptor>(fragmentDescriptor.ClassName));
            }
        }
    }
}
