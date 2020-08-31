#nullable enable

namespace HotChocolate.Types
{
    public interface IHasDescription
    {
        /// <summary>
        /// Gets the description of the object.
        /// </summary>
        string? Description { get; }
    }
}
