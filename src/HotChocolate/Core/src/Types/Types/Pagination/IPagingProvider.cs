using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Represents an paging provider, which can be implemented to 
    /// create optimized pagination for data sources. 
    /// 
    /// The paging provider will be used by the configuration to choose 
    /// the right paging handler for executing the field.
    /// </summary>
    public interface IPagingProvider
    {
        /// <summary>
        /// Specifies if this paging provider can handle the specified <see cref="source"/>.
        /// </summary>
        /// <param name="source">
        /// The source type represents the result of the field resolver and could be a collection, 
        /// a query builder or some other object representing the data set.
        /// </param>
        bool CanHandle(IExtendedType source);

        /// <summary>
        /// Creates the paging handler that is able to interact with the specified source and 
        /// able to slice the data.
        /// </summary>
        /// <param name="source">
        /// The source type represents the result of the field resolver and could be a collection, 
        /// a query builder or some other object representing the data set.
        /// </param>
        /// <param name="options">
        /// The paging settings that apply to the newly create paging handler.
        /// </param>
        /// <returns>
        /// Returns the paging handler that was create for the specified <paramref name="source"/>.
        /// </returns>
        IPagingHandler CreateHandler(IExtendedType source, PagingOptions options);
    }
}
