#nullable enable

namespace HotChocolate.Types
{
    public interface IHasDescription
    {
        string? Description { get; }
    }

    public interface IHasScope
    {
        string? Scope { get; }
    }
}
