namespace HotChocolate.Resolvers
{
    /// <summary>
    /// A reference to a specific field of a GraphQL schema.
    /// </summary>
    public interface IFieldReference
    {
        /// <summary>
        /// The name of a GraphQL object type.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// The name of a field of the object type.
        /// </summary>
        string FieldName { get; }
    }
}
