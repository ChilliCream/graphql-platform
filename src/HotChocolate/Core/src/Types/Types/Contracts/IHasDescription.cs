#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// GraphQL type system members that have a description.
    /// </summary>
    public interface IHasDescription
    {
        /// <summary>
        /// Gets the description of the object.
        /// </summary>
        string? Description { get; }
    }
}
