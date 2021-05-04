using ConferencePlanner.GraphQL.Data;
using HotChocolate;
using HotChocolate.Types.Relay;

namespace ConferencePlanner.GraphQL.Speakers
{
    public record ModifySpeakerInput(
        [ID(nameof(Speaker))] 
        int Id,
        Optional<string?> Name,
        Optional<string?> Bio,
        Optional<string?> WebSite);
}