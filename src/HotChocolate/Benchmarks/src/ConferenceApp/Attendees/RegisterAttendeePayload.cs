using HotChocolate.ConferencePlanner.Common;
using HotChocolate.ConferencePlanner.Data;

namespace HotChocolate.ConferencePlanner.Attendees
{
    public class RegisterAttendeePayload : AttendeePayloadBase
    {
        public RegisterAttendeePayload(Attendee attendee)
            : base(attendee)
        {
        }

        public RegisterAttendeePayload(UserError error)
            : base(new[] { error })
        {
        }
    }
}