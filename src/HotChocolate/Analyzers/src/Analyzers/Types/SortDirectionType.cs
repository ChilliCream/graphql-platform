using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class SortDirectionType : EnumType<SortDirection>
    {
        protected override void Configure(IEnumTypeDescriptor<SortDirection> descriptor)
        {
            descriptor.Name("_SortDirection");
        }
    }
}
