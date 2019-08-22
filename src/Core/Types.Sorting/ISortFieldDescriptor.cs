namespace HotChocolate.Types.Sorting
{
    public interface ISortFieldDescriptor
    {
        /// <summary>
        /// Specify the name of the sort operation.
        /// </summary>
        /// <param name="value">
        ///  The sort operation name.
        /// </param>
        ISortFieldDescriptor Name(NameString value);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that shall be ignored.</param>

        ISortFieldDescriptor Ignore();
    }
}
