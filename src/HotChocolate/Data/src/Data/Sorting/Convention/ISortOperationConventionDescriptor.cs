namespace HotChocolate.Data.Sorting
{
    public interface ISortOperationConventionDescriptor
        : IFluent
    {
        /// <summary>
        /// Specify the name of the operation.
        /// </summary>
        /// <param name="name">
        /// The operation name.
        /// </param>
        ISortOperationConventionDescriptor Name(string name);

        /// <summary>
        /// Specify the description of the operation
        /// </summary>
        /// <param name="description">
        /// The operation description
        /// </param>
        ISortOperationConventionDescriptor Description(string description);
    }
}
