using HotChocolate.ConferencePlanner.Data;
using HotChocolate;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Speakers
{
    public record ModifySpeakerInput(
        [ID(nameof(Speaker))] 
        int Id,
        Optional<string?> Name,
        Optional<string?> Bio,
        Optional<string?> WebSite);
}