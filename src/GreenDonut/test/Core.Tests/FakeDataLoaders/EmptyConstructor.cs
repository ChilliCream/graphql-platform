using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut.FakeDataLoaders
{
    internal class EmptyConstructor
        : DataLoaderBase<string, string>
    {
        internal EmptyConstructor()
            : base()
        { }

        protected override Task<IReadOnlyList<Result<string>>> FetchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
