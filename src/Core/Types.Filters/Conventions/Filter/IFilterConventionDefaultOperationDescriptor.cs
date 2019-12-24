namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionDefaultOperationDescriptor
        : IFilterConventionOperationDescriptorBase
    {
        new IFilterConventionDefaultOperationDescriptor Name(CreateFieldName factory);

        new IFilterConventionDefaultOperationDescriptor Description(string value);

        /// <summary>
        /// Ignores the filter if true
        /// </summary> 
        /// 
        new IFilterConventionDefaultOperationDescriptor Ignore(bool ignore = true);

        IFilterConventionDescriptor And();
    }
}
