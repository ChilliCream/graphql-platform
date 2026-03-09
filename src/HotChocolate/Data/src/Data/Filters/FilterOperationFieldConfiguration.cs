namespace HotChocolate.Data.Filters;

public class FilterOperationFieldConfiguration : FilterFieldConfiguration
{
    public int Id { get; set; }

    internal void CopyTo(FilterOperationFieldConfiguration target)
    {
        base.CopyTo(target);
        target.Id = Id;
    }

    internal void MergeInto(FilterOperationFieldConfiguration target)
    {
        base.MergeInto(target);
        target.Id = Id;
    }
}
