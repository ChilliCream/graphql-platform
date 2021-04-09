namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Represents definitions that carry a ignore flag.
    /// </summary>
    public interface IHasIgnore
    {
        /// <summary>
        /// Defines if this field is ignored and will
        /// not be included into the schema.
        /// </summary>
        bool Ignore { get; }
    }
}
