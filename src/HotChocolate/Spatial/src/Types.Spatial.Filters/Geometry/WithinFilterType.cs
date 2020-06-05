using HotChocolate.Types.Filters;
using HotChocolate.Types.Spatial;

namespace HotChocolate.Types.Spatial.Filters
{
    public class WithinFilterType : FilterInputType<FilterWithin>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FilterWithin> descriptor)
        {
            descriptor.Skip(x => x.Shape).Type<GeoJSONPointInput>();
            // TODO: How to make this allow multiple input types?
            // it would suck to have to have a with_point, with_multipoint, with_x


            // descriptor.Skip(x => x.With).Type<GeoJSONLineStringInput>();
            // descriptor.Skip(x => x.With).Type<GeoJSONPolygonInput>();
        }
    }
}
