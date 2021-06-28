namespace HotChocolate.Data.Sorting
{
    public class DefaultSortEnumType : SortEnumType
    {
        protected override void Configure(ISortEnumTypeDescriptor descriptor)
        {
            descriptor.Name("SortEnumType");
            descriptor.Operation(DefaultSortOperations.Ascending);
            descriptor.Operation(DefaultSortOperations.Descending);
        }
    }
}
