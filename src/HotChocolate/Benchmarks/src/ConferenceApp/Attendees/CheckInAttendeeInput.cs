using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Attendees
{
    public record CheckInAttendeeInput(
        [GlobalId(nameof(Session))]
        int SessionId,
        [GlobalId(nameof(Attendee))]
        int AttendeeId);
}