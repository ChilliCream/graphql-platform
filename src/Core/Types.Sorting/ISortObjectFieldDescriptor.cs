namespace HotChocolate.Types.Sorting
{
    public interface ISortObjectFieldDescriptor
    {
        /// <summary>
        /// Specify the name of the sort operation.
        /// </summary>
        /// <param name="value">
        ///  The sort operation name.
        /// </param>
        ISortObjectFieldDescriptor Name(NameString value);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that shall be ignored.</param>

        ISortObjectFieldDescriptor Ignore();


        /// <summary>
        /// Allowes sorting of a nested property
        /// </summary> 
        ISortObjectFieldDescriptor AllowSort();
    }
}
