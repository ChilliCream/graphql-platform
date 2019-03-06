namespace HotChocolate.Types
{
    public interface IField
        : IHasName
        , IHasDescription
        , IHasDirectives
    {
        /// <summary>
        /// The type of which declares this field.
        /// </summary>
        IHasName DeclaringType { get; }
    }
}
