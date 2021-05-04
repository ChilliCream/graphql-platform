using ConferencePlanner.GraphQL.Data;
using HotChocolate.Types.Relay;

namespace ConferencePlanner.GraphQL.Tracks
{
    public record RenameTrackInput([ID(nameof(Track))] int Id, string Name);
}