using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class MapDirectiveType : AggregationDirectiveType<MapDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<MapDirective> descriptor)
        {
            descriptor.Name("map");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(MapDirective directive)
        {
            return new MapOperation(directive.Key);
        }
    }
}
