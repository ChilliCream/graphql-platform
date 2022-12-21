using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.ConferencePlanner.Attendees
{
    [Node]
    [ExtendObjectType(typeof(Attendee))]
    public class AttendeeExtensions
    {
        [BindMember(nameof(Attendee.SessionsAttendees))]
        public async Task<IEnumerable<Session>> GetSessionsAsync(
            [Parent] Attendee attendee,
            [ScopedService] ApplicationDbContext dbContext,
            SessionByIdDataLoader sessionById,
            CancellationToken cancellationToken)
        {
            var speakerIds = await dbContext.Attendees
                .Where(a => a.Id == attendee.Id)
                .Include(a => a.SessionsAttendees)
                .SelectMany(a => a.SessionsAttendees.Select(t => t.SessionId))
                .ToArrayAsync(cancellationToken);

            return await sessionById.LoadAsync(speakerIds, cancellationToken);
        }

        [NodeResolver]
        public Task<Attendee> GetAttendeeAsync(
            AttendeeByIdDataLoader attendeeById,
            int id,
            CancellationToken cancellationToken) =>
            attendeeById.LoadAsync(id, cancellationToken);
    }
}