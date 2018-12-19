namespace HotChocolate.Types
{
    public interface IField
    {
        /// <summary>
        /// The type of which declares this field.
        /// </summary>
        INamedType DeclaringType { get; }

        /// <summary>
        /// The name of the field
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The description of the field
        /// </summary>
        string Description { get; }
    }
}
