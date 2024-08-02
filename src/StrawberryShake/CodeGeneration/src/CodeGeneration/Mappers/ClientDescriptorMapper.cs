using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class ClientDescriptorMapper
{
    public static void Map(IMapperContext context)
        => context.Register(
            new ClientDescriptor(
                context.ClientName,
                context.Namespace,
                context.Operations.ToList()));
}
