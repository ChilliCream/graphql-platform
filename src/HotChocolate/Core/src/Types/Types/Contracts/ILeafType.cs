namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL leaf-type e.g. scalar or enum.
    /// </summary>
    public interface ILeafType
        : INamedOutputType
        , INamedInputType
    {
    }
}
