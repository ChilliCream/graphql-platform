namespace HotChocolate.Data.Filters
{
    public interface IFilterOperationConventionDescriptor
        : IFluent
    {
        /// <summary>
        /// Specify the name of the operation.
        /// </summary>
        /// <param name="name">
        /// The operation name.
        /// </param>
        IFilterOperationConventionDescriptor Name(string name);

        /// <summary>
        /// Specify the description of the operation
        /// </summary>
        /// <param name="description">
        /// The operation description
        /// </param>
        IFilterOperationConventionDescriptor Description(string description);
    }
}
