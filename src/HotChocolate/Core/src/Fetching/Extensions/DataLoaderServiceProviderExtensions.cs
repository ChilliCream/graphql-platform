using System;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

internal static class DataLoaderServiceProviderExtensions
{
    public static void InitializeDataLoaderScope(this IServiceProvider services)
    {
        var dataLoaderScope = services.GetRequiredService<DataLoaderScopeHolder>().PinNewScope(services);

        // the pinned scope and the scope in the DI must match ... otherwise we fail here!
        if (!ReferenceEquals(dataLoaderScope, services.GetRequiredService<IDataLoaderScope>()))
        {
            throw new InvalidOperationException("The DataLoaderScope has an inconsistent state.");
        }
    }
}