using ConferencePlanner.GraphQL.Common;
using ConferencePlanner.GraphQL.Data;

namespace ConferencePlanner.GraphQL.Sessions
{
    public class AddSessionPayload : Payload
    {
        public AddSessionPayload(Session session)
        {
            Session = session;
        }

        public AddSessionPayload(UserError error)
            : base(new[] { error })
        {
        }

        public Session? Session { get; }
    }
}