using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotChocolate.RateLimit
{
    public interface IRateLimitContext
    {
        Task<RequestIdentity> CreateRequestIdentityAsync(
            IReadOnlyCollection<IPolicyIdentifier> identifiers, Path path);
    }
}
