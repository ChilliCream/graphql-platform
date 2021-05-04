using System.Collections.Generic;
using ConferencePlanner.GraphQL.Common;
using ConferencePlanner.GraphQL.Data;

namespace ConferencePlanner.GraphQL.Tracks
{
    public class AddTrackPayload : TrackPayloadBase
    {
        public AddTrackPayload(Track track) 
            : base(track)
        {
        }

        public AddTrackPayload(IReadOnlyList<UserError> errors) 
            : base(errors)
        {
        }
    }
}