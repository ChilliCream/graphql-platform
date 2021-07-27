using System.Collections.Generic;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Sessions
{
    public record AddSessionInput(
        string Title,
        string? Abstract,
        [GlobalId(nameof(Speaker))]
        IReadOnlyList<int> SpeakerIds);
}