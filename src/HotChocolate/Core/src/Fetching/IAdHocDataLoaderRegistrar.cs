using System;
using GreenDonut;

namespace HotChocolate.Fetching
{
    public interface IAdHocDataLoaderRegistrar
    {
        TDataLoader GetOrAddDataLoader<TDataLoader>(
            string name,
            Func<IBatchScheduler, TDataLoader> factory)
            where TDataLoader : IDataLoader;
    }
}
