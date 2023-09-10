using HotChocolate.ConferencePlanner.Common;
using HotChocolate.ConferencePlanner.Data;

namespace HotChocolate.ConferencePlanner.Speakers
{
    public class ModifySpeakerPayload : SpeakerPayloadBase
    {
        public ModifySpeakerPayload(Speaker speaker)
            : base(speaker)
        {
        }

        public ModifySpeakerPayload(UserError error)
            : base(new [] { error })
        {
        }
    }
}