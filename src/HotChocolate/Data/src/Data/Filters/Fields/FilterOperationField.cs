namespace HotChocolate.Data.Filters
{
    public sealed class FilterOperationField
        : FilterField
        , IFilterOperationField
    {
        internal FilterOperationField(
            FilterOperationFieldDefinition definition)
            : base(definition)
        {
            Id = definition.Id;
        }

        public int Id { get; }
    }
}
