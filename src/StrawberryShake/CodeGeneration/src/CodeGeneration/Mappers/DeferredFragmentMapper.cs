using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class DeferredFragmentMapper
{
    public static void Map(IMapperContext context)
    {
        foreach (ComplexTypeDescriptor complexType in context.Types.OfType<ComplexTypeDescriptor>())
        {
            foreach (DeferredFragmentDescriptor fragmentDescriptor in complexType.Deferred)
            {
                fragmentDescriptor.Complete(
                    context.GetType<InterfaceTypeDescriptor>(fragmentDescriptor.InterfaceName),
                    context.GetType<ObjectTypeDescriptor>(fragmentDescriptor.ClassName));
            }
        }
    }
}
