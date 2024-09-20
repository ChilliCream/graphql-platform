using GreenDonut;

namespace HotChocolate.Fetching;

public interface IDataLoaderScopeFactory
{
    void BeginScope(IBatchScheduler? scheduler = default);
}
