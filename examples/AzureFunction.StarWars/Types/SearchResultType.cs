using HotChocolate.Types;
using StarWars.Models;

namespace StarWars.Types
{
    public class SearchResultType
        : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("SearchResult");
            descriptor.Type<ObjectType<Starship>>();
            descriptor.Type<HumanType>();
            descriptor.Type<DroidType>();
        }
    }
}
