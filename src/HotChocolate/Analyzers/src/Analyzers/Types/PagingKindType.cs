using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types
{
    public class PagingKindType : EnumType<PagingKind>
    {
        protected override void Configure(IEnumTypeDescriptor<PagingKind> descriptor)
        {
            descriptor.Name("_PagingKind");
        }
    }
}
