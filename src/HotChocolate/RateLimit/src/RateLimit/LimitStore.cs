using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace HotChocolate.RateLimit
{
    internal class LimitStore : ILimitStore
    {
        private readonly IDistributedCache _cache;

        public LimitStore(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SetAsync(
            string key,
            TimeSpan expiration,
            Limit limit,
            CancellationToken cancellationToken)
        {
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetAsync(key, limit.ToByte(), cacheEntryOptions, cancellationToken);
        }

        public async Task<Limit> TryGetAsync(
            string key,
            CancellationToken cancellationToken)
        {
            byte[] payload = await _cache.GetAsync(key, cancellationToken);

            return payload?.ToLimit() ?? Limit.Empty;
        }
    }
}
