using System;
using GreenDonut.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

internal sealed class DataLoaderContextHelper(IServiceProvider services) : IDataLoaderContextHelper
{
    public IDataLoaderContext CreateNewContext()
    {
        var holder = services.GetRequiredService<DataLoaderContextHolder>();
        var context = holder.PinNewScope(services);

        // the pinned scope and the scope in the DI must match ... otherwise we fail here!
        if (!ReferenceEquals(context, services.GetRequiredService<IDataLoaderContext>()))
        {
            throw new InvalidOperationException("The DataLoaderScope has an inconsistent state.");
        }
        
        return context;
    }

    public IDataLoaderContext EnsureContextExists()
    {
        var holder = services.GetRequiredService<DataLoaderContextHolder>();
        var context = holder.GetOrCreateContext(services);

        // the pinned scope and the scope in the DI must match ... otherwise we fail here!
        if (!ReferenceEquals(context, services.GetRequiredService<IDataLoaderContext>()))
        {
            throw new InvalidOperationException("The DataLoaderScope has an inconsistent state.");
        }
        
        return context;
    }
}