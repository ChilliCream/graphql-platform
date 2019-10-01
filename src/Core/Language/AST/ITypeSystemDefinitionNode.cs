namespace HotChocolate.Language
{
    /// <summary>
    /// The GraphQL language includes an IDL used to describe a GraphQL service’s
    /// type system. Tools may use this definition language to provide utilities
    /// such as client code generation or service boot‐strapping.
    /// </summary>
    public interface ITypeSystemDefinitionNode
        : IDefinitionNode
    {
    }
}
