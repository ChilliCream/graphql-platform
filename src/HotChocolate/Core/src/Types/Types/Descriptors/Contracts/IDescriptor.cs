namespace HotChocolate.Types
{
    /// <summary>
    /// A descriptor is used to specify the configuration of a type system member.
    /// </summary>
    public interface IDescriptor
    {
        /// <summary>
        /// Allows for type system member configuration methods to modify the underlying
        /// type system member definition object.
        /// </summary>
        IDescriptorExtension Extend();
    }
}
