using System.Collections.Generic;
using ConferencePlanner.GraphQL.Common;
using ConferencePlanner.GraphQL.Data;

namespace ConferencePlanner.GraphQL.Attendees
{
    public class AttendeePayloadBase : Payload
    {
        protected AttendeePayloadBase(Attendee attendee)
        {
        }

        protected AttendeePayloadBase(IReadOnlyList<UserError> errors)
            : base(errors)
        {
        }

        public Attendee? Attendee { get; }
    }
}