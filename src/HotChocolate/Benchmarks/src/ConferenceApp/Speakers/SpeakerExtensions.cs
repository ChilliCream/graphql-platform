using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Speakers
{
    [Node]
    [ExtendObjectType(typeof(Speaker))]
    public class SpeakerExtensions
    {
        [UseApplicationDbContext]
        public async Task<IEnumerable<Session>> GetSessionsAsync(
            [Parent] Speaker speaker,
            SessionBySpeakerIdDataLoader sessionBySpeakerId,
            CancellationToken cancellationToken)
            => await sessionBySpeakerId.LoadAsync(speaker.Id, cancellationToken);

        [NodeResolver]
        public Task<Speaker> GetSpeakerAsync(
            SpeakerByIdDataLoader speakerById,
            int id,
            CancellationToken cancellationToken) =>
            speakerById.LoadAsync(id, cancellationToken);
    }
}
