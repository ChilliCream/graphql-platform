#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL input object type
    /// </summary>
    public interface IInputObjectType
        : INamedInputType
    {
        IFieldCollection<IInputField> Fields { get; }
    }
}
