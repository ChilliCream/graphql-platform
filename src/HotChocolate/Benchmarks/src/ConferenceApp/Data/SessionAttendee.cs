namespace ConferencePlanner.GraphQL.Data
{
    public class SessionAttendee
    {
        public int SessionId { get; set; }

        public Session? Session { get; set; }

        public int AttendeeId { get; set; }

        public Attendee? Attendee { get; set; }
    }
}