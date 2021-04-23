namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a named GraphQL type.
    /// </summary>
    public interface INamedType
        : INullableType
        , IHasName
        , IHasDescription
        , IHasSyntaxNode
        , IHasReadOnlyContextData
    {
        /// <summary>
        /// Determines whether an instance of a specified type <paramref name="type" />
        /// can be assigned to a variable of the current type.
        /// </summary>
        bool IsAssignableFrom(INamedType type);
    }
}
