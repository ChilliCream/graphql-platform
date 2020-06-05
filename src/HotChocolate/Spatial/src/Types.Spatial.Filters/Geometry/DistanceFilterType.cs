using HotChocolate.Types.Filters;
using HotChocolate.Types.Spatial;

namespace HotChocolate.Types.Spatial.Filters
{
    public class DistanceFilterType : FilterInputType<FilterDistance>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FilterDistance> descriptor)
        {
            descriptor.Skip(x => x.Shape).Type<GeoJSONPointInput>();
            descriptor.Filter(x => x.Is);
        }
    }
}
