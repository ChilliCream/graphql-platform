using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.RateLimit
{
    public interface ILimitProcessor
    {
        Task<Limit> ExecuteAsync(RequestIdentity requestIdentity,
            LimitPolicy policy,
            CancellationToken cancellationToken);
    }
}
