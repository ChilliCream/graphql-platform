using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class DataLoaderResolverContextExtensions
{
    [GetDataLoader]
    public static T DataLoader<T>(this IResolverContext context)
        where T : IDataLoader
    {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.RequestServices;
        var reg = services.GetRequiredService<IDataLoaderScope>();
        return reg.GetDataLoader<T>();
    }
}
