using ConferencePlanner.GraphQL.Common;
using ConferencePlanner.GraphQL.Data;

namespace ConferencePlanner.GraphQL.Sessions
{
    public class RenameSessionPayload : Payload
    {
        public RenameSessionPayload(Session session)
        {
            Session = session;
        }

        public RenameSessionPayload(UserError error)
            : base(new[] { error })
        {
        }

        public Session? Session { get; }
    }
}