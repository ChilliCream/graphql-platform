using ConferencePlanner.GraphQL.Common;
using ConferencePlanner.GraphQL.Data;

namespace ConferencePlanner.GraphQL.Speakers
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