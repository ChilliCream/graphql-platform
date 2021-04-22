namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL output type which has a name.
    /// </summary>
    public interface INamedOutputType
        : INamedType
        , IOutputType
    {
    }
}
