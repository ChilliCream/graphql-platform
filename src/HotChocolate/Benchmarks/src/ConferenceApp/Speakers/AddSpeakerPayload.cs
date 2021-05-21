using System.Collections.Generic;
using HotChocolate.ConferencePlanner.Common;
using HotChocolate.ConferencePlanner.Data;

namespace HotChocolate.ConferencePlanner.Speakers
{
    public class AddSpeakerPayload : SpeakerPayloadBase
    {
        public AddSpeakerPayload(Speaker speaker)
            : base(speaker)
        {
        }

        public AddSpeakerPayload(IReadOnlyList<UserError> errors)
            : base(errors)
        {
        }
    }
}