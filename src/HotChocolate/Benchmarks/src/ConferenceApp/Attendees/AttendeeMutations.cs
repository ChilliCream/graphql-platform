using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConferencePlanner.GraphQL.Common;
using ConferencePlanner.GraphQL.Data;
using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace ConferencePlanner.GraphQL.Attendees
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class AttendeeMutations
    {
        [UseApplicationDbContext]
        public async Task<RegisterAttendeePayload> RegisterAttendeeAsync(
            RegisterAttendeeInput input,
            [ScopedService] ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            var attendee = new Attendee
            {
                FirstName = input.FirstName,
                LastName = input.LastName,
                UserName = input.UserName,
                EmailAddress = input.EmailAddress
            };

            context.Attendees.Add(attendee);

            await context.SaveChangesAsync(cancellationToken);

            return new RegisterAttendeePayload(attendee);
        }

        [UseApplicationDbContext]
        public async Task<CheckInAttendeePayload> CheckInAttendeeAsync(
            CheckInAttendeeInput input,
            [ScopedService] ApplicationDbContext context,
            [Service] ITopicEventSender eventSender,
            CancellationToken cancellationToken)
        {
            Attendee attendee = await context.Attendees.FirstOrDefaultAsync(
                t => t.Id == input.AttendeeId, cancellationToken);

            if (attendee is null)
            {
                return new CheckInAttendeePayload(
                    new UserError("Attendee not found.", "ATTENDEE_NOT_FOUND"));
            }

            attendee.SessionsAttendees.Add(
                new SessionAttendee
                {
                    SessionId = input.SessionId
                });

            await context.SaveChangesAsync(cancellationToken);

            await eventSender.SendAsync(
                "OnAttendeeCheckedIn_" + input.SessionId,
                input.AttendeeId,
                cancellationToken);

            return new CheckInAttendeePayload(attendee, input.SessionId);
        }
    }
}