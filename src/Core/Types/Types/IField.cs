namespace HotChocolate.Types
{
    public interface IField
        : IHasName
        , IHasDescription
    {
        /// <summary>
        /// The type of which declares this field.
        /// </summary>
        IHasName DeclaringType { get; }
    }
}
