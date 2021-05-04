using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Attendees
{
    public record CheckInAttendeeInput(
        [ID(nameof(Session))]
        int SessionId,
        [ID(nameof(Attendee))]
        int AttendeeId);
}