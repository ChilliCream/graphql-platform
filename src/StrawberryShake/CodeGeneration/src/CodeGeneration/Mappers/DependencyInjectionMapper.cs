using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class DependencyInjectionMapper
{
    public static void Map(IMapperContext context)
    {
        context.Register(
            new DependencyInjectionDescriptor(
                context.Client,
                context.EntityTypes.ToList(),
                context.Operations.ToList(),
                context.Types,
                context.TransportProfiles,
                context.EntityIdFactory,
                context.StoreAccessor,
                context.ResultFromEntityMappers));
    }
}
