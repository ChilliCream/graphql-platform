namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionDefaultOperationDescriptor
        : IFilterConventionOperationDescriptorBase
    {
        /// <inheritdoc/>
        new IFilterConventionDefaultOperationDescriptor Name(CreateFieldName factory);

        /// <inheritdoc/>
        new IFilterConventionDefaultOperationDescriptor Description(string value);

        /// <summary>
        /// Add additional configuration to <see cref="IFilterConventionDescriptor"/>
        /// </summary>
        IFilterConventionDescriptor And();
    }
}
