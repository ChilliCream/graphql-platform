using HotChocolate.Types.Filters;
using HotChocolate.Types.Spatial;

namespace HotChocolate.Types.Spatial.Filters
{
    public class AreaFilterType : FilterInputType<FilterArea>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FilterArea> descriptor)
        {
            descriptor.Filter(x => x.Is);
        }
    }
}
