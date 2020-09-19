using System.Collections.Generic;

namespace HotChocolate.RateLimit
{
    public interface ILimitContext
    {
        RequestIdentity CreateRequestIdentity(
            IReadOnlyCollection<IPolicyIdentifier> identifiers, Path path);
    }
}
