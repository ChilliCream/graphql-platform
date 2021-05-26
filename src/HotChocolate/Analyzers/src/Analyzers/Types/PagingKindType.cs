using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class PagingKindType : EnumType<PagingKind>
    {
        protected override void Configure(IEnumTypeDescriptor<PagingKind> descriptor)
        {
            descriptor.Name("_PagingKind");
        }
    }
}
