using HotChocolate.ConferencePlanner.Common;
using HotChocolate.ConferencePlanner.Data;

namespace HotChocolate.ConferencePlanner.Sessions
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