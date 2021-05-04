using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate.ConferencePlanner.Types;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Sessions
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class SessionQueries
    {
        [UseApplicationDbContext]
        [UsePaging]
        [UseFiltering(typeof(SessionFilterInputType))]
        [UseSorting]
        public IQueryable<Session> GetSessions(
            [ScopedService] ApplicationDbContext context) =>
            context.Sessions;

        public Task<Session> GetSessionByIdAsync(
            [ID(nameof(Session))] int id,
            SessionByIdDataLoader sessionById,
            CancellationToken cancellationToken) =>
            sessionById.LoadAsync(id, cancellationToken);

        public async Task<IEnumerable<Session>> GetSessionsByIdAsync(
            [ID(nameof(Session))] int[] ids,
            SessionByIdDataLoader sessionById,
            CancellationToken cancellationToken) =>
            await sessionById.LoadAsync(ids, cancellationToken);
    }
}