namespace HotChocolate.Types
{
    /// <summary>
    /// GraphQL type system members that have a name.
    /// </summary>
    public interface IHasName
    {
        /// <summary>
        /// Gets the GraphQL type system member name.
        /// </summary>
        NameString Name { get; }
    }
}
