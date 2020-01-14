namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionDefaultOperationDescriptor
        : IFilterConventionOperationDescriptorBase
    {
        new IFilterConventionDefaultOperationDescriptor Name(CreateFieldName factory);

        new IFilterConventionDefaultOperationDescriptor Description(string value);

        IFilterConventionDescriptor And();
    }
}
