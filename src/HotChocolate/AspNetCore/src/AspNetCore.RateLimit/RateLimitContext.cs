using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.RateLimit;

namespace HotChocolate.AspNetCore.RateLimit
{
    internal class RateLimitContext : IRateLimitContext
    {
        private readonly IServiceProvider _serviceProvider;

        public RateLimitContext(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<RequestIdentity> CreateRequestIdentityAsync(
            IReadOnlyCollection<IPolicyIdentifier> identifiers, Path path)
        {
            string[] resolvedIdentifiers = ArrayPool<string>.Shared.Rent(identifiers.Count);

            try
            {
                for (var i = 0; i < identifiers.Count; i++)
                {
                    resolvedIdentifiers[i] = await identifiers.ElementAt(i)
                        .ResolveAsync(_serviceProvider);
                }

                return RequestIdentity.Create(path.ToString(), resolvedIdentifiers);
            }
            finally
            {
                ArrayPool<string>.Shared.Return(resolvedIdentifiers);
            }
        }
    }
}
