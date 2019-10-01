namespace HotChocolate.Language
{
    /// <summary>
    /// Type system extensions are used to represent a GraphQL type system which
    /// has been extended from some original type system. For example, this might
    /// be used by a local service to represent data a GraphQL client only accesses
    /// locally, or by a GraphQL service which is itself an extension of another GraphQL
    /// service.
    ///
    /// https://graphql.github.io/graphql-spec/June2018/#sec-Type-System-Extensions
    /// </summary>
    public interface ITypeSystemExtensionNode
        : IDefinitionNode
    {
    }
}
