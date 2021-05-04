using System.Collections.Generic;
using HotChocolate.ConferencePlanner.Common;
using HotChocolate.ConferencePlanner.Data;

namespace HotChocolate.ConferencePlanner.Tracks
{
    public class RenameTrackPayload : TrackPayloadBase
    {
        public RenameTrackPayload(Track track) 
            : base(track)
        {
        }

        public RenameTrackPayload(IReadOnlyList<UserError> errors) 
            : base(errors)
        {
        }
    }
}