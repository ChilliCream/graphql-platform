using HotChocolate.Types.Filters;
using HotChocolate.Types.Spatial;

namespace HotChocolate.Types.Spatial.Filters
{
    public class LengthFilterType : FilterInputType<FilterLength>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FilterLength> descriptor)
        {
            descriptor.Filter(x => x.Is);
        }
    }
}
