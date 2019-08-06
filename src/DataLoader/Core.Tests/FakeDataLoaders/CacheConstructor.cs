using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut.FakeDataLoaders
{
    internal class CacheConstructor
        : DataLoaderBase<string, string>
    {
        internal CacheConstructor(TaskCache<string> cache)
            : base(cache)
        { }

        protected override Task<IReadOnlyList<Result<string>>> FetchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
