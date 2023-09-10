using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Tracks
{
    public record RenameTrackInput([ID(nameof(Track))] int Id, string Name);
}