#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL input type which has a name.
    /// </summary>
    public interface INamedInputType
        : INamedType
        , IInputType
    {
    }
}
