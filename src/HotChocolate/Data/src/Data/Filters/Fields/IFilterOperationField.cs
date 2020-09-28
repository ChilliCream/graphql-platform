namespace HotChocolate.Data.Filters
{
    public interface IFilterOperationField
        : IFilterField
    {
        /// <summary>
        /// Gets the internal operation ID.
        /// </summary>
        int Id { get; }
    }
}
