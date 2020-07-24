namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL object type
    /// </summary>
    public interface IObjectType : IComplexOutputType
    {
        /// <summary>
        /// Gets the field that the type exposes.
        /// </summary>
        new IFieldCollection<IObjectField> Fields { get; }
    }
}
