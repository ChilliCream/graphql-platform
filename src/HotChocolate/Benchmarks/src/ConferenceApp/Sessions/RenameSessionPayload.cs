using HotChocolate.ConferencePlanner.Common;
using HotChocolate.ConferencePlanner.Data;

namespace HotChocolate.ConferencePlanner.Sessions
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