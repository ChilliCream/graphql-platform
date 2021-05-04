using System.Collections.Generic;
using ConferencePlanner.GraphQL.Data;
using HotChocolate.Types.Relay;

namespace ConferencePlanner.GraphQL.Sessions
{
    public record AddSessionInput(
        string Title,
        string? Abstract,
        [ID(nameof(Speaker))]
        IReadOnlyList<int> SpeakerIds);
}