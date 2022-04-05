namespace HotChocolate.Data.Filters;

public class FilterOperationField
    : FilterField
    , IFilterOperationField
{
    internal FilterOperationField(
        FilterOperationFieldDefinition definition)
        : this(definition, default)
    {
    }

    internal FilterOperationField(FilterOperationFieldDefinition definition, int index)
        : base(definition, index)
    {
        Id = definition.Id;
    }

    public int Id { get; }
}
