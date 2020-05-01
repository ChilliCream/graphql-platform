using HotChocolate.Types.Filters;

namespace Filtering.Customization
{
    public class UserFilterType : FilterInputType<User>
    {
        protected override void Configure(IFilterInputTypeDescriptor<User> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Filter(x => x.SignedUp).BindFiltersExplicitly().AllowFrom();
        } 
    }
}