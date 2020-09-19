using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.RateLimit
{
    internal class LimitProcessor : ILimitProcessor
    {
        private readonly ILimitStore _limitStore;

        public LimitProcessor(ILimitStore limitStore)
        {
            _limitStore = limitStore;
        }

        public async Task<Limit> ExecuteAsync(
            RequestIdentity requestIdentity,
            LimitPolicy policy,
            CancellationToken cancellationToken)
        {
            Limit limit = Limit.One;

            IDisposable sync = null!;
            using (sync) // TODO build distributed sync per requestIdentity
            {
                Limit currentLimit = await _limitStore
                    .TryGetAsync(requestIdentity, cancellationToken);

                if (!currentLimit.IsExpired(policy))
                {
                    limit = currentLimit.Increment();
                }

                await _limitStore
                    .SetAsync(requestIdentity, policy.Period, limit, cancellationToken);
            }

            return limit;
        }
    }
}
