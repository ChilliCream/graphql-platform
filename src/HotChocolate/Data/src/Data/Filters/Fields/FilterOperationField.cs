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
            Operation = definition.Operation;
        }

        public int Operation { get; }
    }
}
