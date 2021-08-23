using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
{
    public class PagingKindType : EnumType<PagingKind>
    {
        protected override void Configure(IEnumTypeDescriptor<PagingKind> descriptor)
        {
            descriptor.Name("_PagingKind");
        }
    }
}
