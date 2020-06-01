using HotChocolate.Types.Filters;

namespace HotChocolate.Spatial.Types.Filters
{
    public class DistanceFilterType
        : FilterInputType<FilterDistance>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<FilterDistance> descriptor)
        {
            descriptor.Input(x => x.From);
            descriptor.Filter(x => x.Is);
        }
    }
}
