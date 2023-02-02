using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.ConferencePlanner.Sessions
{
    [Node]
    [ExtendObjectType(typeof(Session))]
    public class SessionExtensions
    {
        [UseApplicationDbContext]
        [BindMember(nameof(Session.SessionSpeakers))]
        public async Task<IEnumerable<Speaker>> GetSpeakersAsync(
            [Parent] Session session,
            SpeakerBySessionIdDataLoader speakerById,
            CancellationToken cancellationToken)
            => await speakerById.LoadAsync(session.Id, cancellationToken);

        [UseApplicationDbContext]
        [BindMember(nameof(Session.SessionAttendees))]
        public async Task<IEnumerable<Attendee>> GetAttendeesAsync(
            [Parent] Session session,
            [ScopedService] ApplicationDbContext dbContext,
            AttendeeByIdDataLoader attendeeById,
            CancellationToken cancellationToken)
        {
            var attendeeIds = await dbContext.Sessions
                .Where(s => s.Id == session.Id)
                .Include(s => s.SessionAttendees)
                .SelectMany(s => s.SessionAttendees.Select(t => t.AttendeeId))
                .ToArrayAsync(cancellationToken);

            return await attendeeById.LoadAsync(attendeeIds, cancellationToken);
        }

        [BindMember(nameof(Session.Track))]
        public async Task<Track?> GetTrackAsync(
            [Parent] Session session,
            TrackByIdDataLoader trackById,
            CancellationToken cancellationToken)
        {
            if (session.TrackId is null)
            {
                return null;
            }

            return await trackById.LoadAsync(session.TrackId.Value, cancellationToken);
        }

        [ID(nameof(Track))]
        [BindMember(nameof(Session.TrackId))]
        public int? TrackId([Parent] Session session) => session.TrackId;

        [NodeResolver]
        public Task<Session> GetSessionAsync(
            SessionByIdDataLoader sessionById,
            int id,
            CancellationToken cancellationToken) =>
            sessionById.LoadAsync(id, cancellationToken);
    }
}
