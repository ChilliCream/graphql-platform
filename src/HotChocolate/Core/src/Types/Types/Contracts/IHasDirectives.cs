#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// GraphQL type system members that have directives.
    /// </summary>
    public interface IHasDirectives
    {
        /// <summary>
        /// Gets the directives of this type system member.
        /// </summary>
        public IDirectiveCollection Directives { get; }
    }
}
