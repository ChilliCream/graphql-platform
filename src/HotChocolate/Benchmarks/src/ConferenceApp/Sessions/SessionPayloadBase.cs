using System.Collections.Generic;
using HotChocolate.ConferencePlanner.Common;
using HotChocolate.ConferencePlanner.Data;

namespace HotChocolate.ConferencePlanner.Sessions
{
    public class SessionPayloadBase : Payload
    {
        protected SessionPayloadBase(Session session)
        {
            Session = session;
        }

        protected SessionPayloadBase(IReadOnlyList<UserError> errors)
            : base(errors)
        {
        }

        public Session? Session { get; }
    }
}