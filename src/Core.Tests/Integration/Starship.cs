using HotChocolate.Types;

namespace HotChocolate.Integration
{
    public class Starship
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Length { get; set; }
    }

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
