using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Sessions
{
    public record RenameSessionInput(
        [GlobalId(nameof(Session))] int SessionId,
        string Title);
}