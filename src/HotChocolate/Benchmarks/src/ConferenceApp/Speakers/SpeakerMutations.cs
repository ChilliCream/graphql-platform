using System.Threading;
using System.Threading.Tasks;
using ConferencePlanner.GraphQL.Common;
using ConferencePlanner.GraphQL.Data;
using HotChocolate;
using HotChocolate.Types;

namespace ConferencePlanner.GraphQL.Speakers
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class SpeakerMutations
    {
        [UseApplicationDbContext]
        public async Task<AddSpeakerPayload> AddSpeakerAsync(
            AddSpeakerInput input,
            [ScopedService] ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            var speaker = new Speaker
            {
                Name = input.Name,
                Bio = input.Bio,
                WebSite = input.WebSite
            };

            context.Speakers.Add(speaker);
            await context.SaveChangesAsync(cancellationToken);

            return new AddSpeakerPayload(speaker);
        }

        [UseApplicationDbContext]
        public async Task<ModifySpeakerPayload> ModifySpeakerAsync(
            ModifySpeakerInput input,
            [ScopedService] ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            if (input.Name.HasValue && input.Name.Value is null)
            {
                return new ModifySpeakerPayload(
                    new UserError("Name cannot be null", "NAME_NULL"));
            }

            Speaker? speaker = await context.Speakers.FindAsync(input.Id);

            if (speaker is null)
            {
                return new ModifySpeakerPayload(
                    new UserError("Speaker with id not found.", "SPEAKER_NOT_FOUND"));
            }

            if (input.Name.HasValue)
            {
                speaker.Name = input.Name;
            }

            if (input.Bio.HasValue)
            {
                speaker.Bio = input.Bio;
            }

            if (input.WebSite.HasValue)
            {
                speaker.WebSite = input.WebSite;
            }

            await context.SaveChangesAsync(cancellationToken);

            return new ModifySpeakerPayload(speaker);
        }
    }
}