using HotChocolate.Types;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars.Types
{
    public class SearchResultType
        : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("SearchResult");
            descriptor.Type<StarshipType>();
            descriptor.Type<HumanType>();
            descriptor.Type<DroidType>();
        }
    }
}
