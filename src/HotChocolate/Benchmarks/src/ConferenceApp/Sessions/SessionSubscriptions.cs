using System.Threading;
using System.Threading.Tasks;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate;
using HotChocolate.Types;

namespace HotChocolate.ConferencePlanner.Sessions
{
    [ExtendObjectType(OperationTypeNames.Subscription)]
    public class SessionSubscriptions
    {
        [Subscribe]
        [Topic]
        public Task<Session> OnSessionScheduledAsync(
            [EventMessage] int sessionId,
            SessionByIdDataLoader sessionById,
            CancellationToken cancellationToken) =>
            sessionById.LoadAsync(sessionId, cancellationToken);
    }
}