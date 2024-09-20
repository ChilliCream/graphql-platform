using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class StoreAccessorMapper
{
    public static void Map(
        ClientModel model,
        IMapperContext context)
    {
        context.Register(
            new StoreAccessorDescriptor(
                NamingConventions.CreateStoreAccessor(context.ClientName),
                NamingConventions.CreateStateNamespace(context.Namespace)));
    }
}
