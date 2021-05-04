using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConferencePlanner.GraphQL.Data;
using ConferencePlanner.GraphQL.DataLoader;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace ConferencePlanner.GraphQL.Speakers
{
    [Node]
    [ExtendObjectType(typeof(Speaker))]   
    public class SpeakerExtensions
    {
        [UseApplicationDbContext]
        public async Task<IEnumerable<Session>> GetSessionsAsync(
            [Parent] Speaker speaker,
            [ScopedService] ApplicationDbContext dbContext,
            SessionByIdDataLoader sessionById,
            CancellationToken cancellationToken)
        {
            int[] speakerIds = await dbContext.Speakers
                .Where(s => s.Id == speaker.Id)
                .Include(s => s.SessionSpeakers)
                .SelectMany(s => s.SessionSpeakers.Select(t => t.SessionId))
                .ToArrayAsync(cancellationToken);

            return await sessionById.LoadAsync(speakerIds, cancellationToken);
        }
    
        [NodeResolver]
        public Task<Speaker> GetSpeakerAsync(
            SpeakerByIdDataLoader speakerById,
            int id,
            CancellationToken cancellationToken) =>
            speakerById.LoadAsync(id, cancellationToken);
    }
}