using System.Collections.Generic;
using ConferencePlanner.GraphQL.Common;
using ConferencePlanner.GraphQL.Data;

namespace ConferencePlanner.GraphQL.Sessions
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