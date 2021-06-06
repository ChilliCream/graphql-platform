using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types
{
    public class OperationKindType : EnumType<OperationKind>
    {
        protected override void Configure(IEnumTypeDescriptor<OperationKind> descriptor)
        {
            descriptor.Name("_OperationKind");
        }
    }
}
