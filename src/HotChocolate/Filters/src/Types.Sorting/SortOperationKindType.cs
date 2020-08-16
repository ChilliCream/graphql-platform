namespace HotChocolate.Types.Sorting
{
    public class SortOperationKindType
        : EnumType<SortOperationKind>
    {
        protected override void Configure(IEnumTypeDescriptor<SortOperationKind> descriptor)
        {
            base.Configure(descriptor);
            descriptor.Value(SortOperationKind.Asc);
            descriptor.Value(SortOperationKind.Desc);
        }
    }
}
