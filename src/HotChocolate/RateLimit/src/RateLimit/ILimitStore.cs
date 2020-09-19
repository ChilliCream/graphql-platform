using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.RateLimit
{
    internal interface ILimitStore
    {
        Task<Limit> TryGetAsync(
            string key,
            CancellationToken cancellationToken);

        Task SetAsync(
            string key,
            TimeSpan expiration,
            Limit limit,
            CancellationToken cancellationToken);
    }
}
