#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL input object type
    /// </summary>
    public interface IInputObjectType
        : INamedInputType
        , IHasDirectives
    {
        /// <summary>
        /// Gets the field that this type exposes.
        /// </summary>
        IFieldCollection<IInputField> Fields { get; }
    }
}
