namespace HotChocolate.Data.Filters;

public class FilterOperationField
    : FilterField
    , IFilterOperationField
{
    internal FilterOperationField(
        FilterOperationFieldConfiguration configuration)
        : this(configuration, 0)
    {
    }

    internal FilterOperationField(FilterOperationFieldConfiguration configuration, int index)
        : base(configuration, index)
    {
        Id = configuration.Id;
    }

    public int Id { get; }
}
