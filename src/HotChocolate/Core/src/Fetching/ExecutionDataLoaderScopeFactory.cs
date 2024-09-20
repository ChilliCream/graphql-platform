using GreenDonut;
using GreenDonut.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

public sealed class ExecutionDataLoaderScopeFactory(IServiceProvider services) : IDataLoaderScopeFactory
{
    public void BeginScope(IBatchScheduler? scheduler = default)
    {
        var batchHandler = scheduler ?? services.GetRequiredService<IBatchScheduler>();
        var dataLoaderScopeHolder = services.GetRequiredService<DataLoaderScopeHolder>();
        var dataLoaderScope = dataLoaderScopeHolder.PinNewScope(services, batchHandler);

        // the pinned scope and the scope in the DI must match ... otherwise we fail here!
        if (!ReferenceEquals(dataLoaderScope, services.GetRequiredService<IDataLoaderScope>()))
        {
            throw new InvalidOperationException("The DataLoaderScope has an inconsistent state.");
        }
    }
}
