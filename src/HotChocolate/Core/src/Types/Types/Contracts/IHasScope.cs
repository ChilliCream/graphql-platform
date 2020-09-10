#nullable enable

namespace HotChocolate.Types
{
    public interface IHasScope
    {
        /// <summary>
        /// Gets a scope name that was provided by an extension.
        /// </summary>
        string? Scope { get; }
    }
}
